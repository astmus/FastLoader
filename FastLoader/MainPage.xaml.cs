using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using FastLoader.Resources;
using System.Diagnostics;
using System.Windows.Input;
using System.IO;
using Microsoft.Phone.Reactive;
using System.Text;
using System.IO.IsolatedStorage;
using System.Threading.Tasks;
using System.IO.Compression;
using FastLoader.Extensions;
using System.Text.RegularExpressions;
using System.Windows.Media;
using MSPToolkit.Encodings;
using Microsoft.Phone.Tasks;

namespace FastLoader
{
	//public struct DomainPagesCount
	//{
	//	public string Domain;
	//	public int CountPages;

	//	public DomainPagesCount(string domain, int countPages = 0)
	//	{
	//		this.Domain = domain;
	//		this.CountPages = 0;
	//	}

	//	public override string ToString()
	//	{
	//		return Domain;
	//	}

	//	public bool IsEmpty()
	//	{
	//		return Domain == String.Empty || Domain == null;
	//	}
	//}

	public partial class MainPage : PhoneApplicationPage
	{
		const string GOOGLE_SEARCH_DOMAIN = "https://www.google.com/search?q=";
		const string START_PAGE = "storagefilestart.html";
		// Constructor
		HttpWebRequest _request;
		string _currentDomain;
		//string _currentFileName;
		Uri _currentPage;
		Stack<Uri> _hystory = new Stack<Uri>();
		//Stack<DomainPagesCount> _domains = new Stack<DomainPagesCount>();
		Dictionary<Uri, String> _uriFileNames = new Dictionary<Uri, string>();
		public MainPage()
		{
			InitializeComponent();
			BuildLocalizedApplicationBar();
			_currentPage = new Uri(START_PAGE, UriKind.Relative);
			browser.Navigate(_currentPage);
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			Scheduler.Dispatcher.Schedule(() => { LayoutRoot.Children.Remove(placeholder); }, TimeSpan.FromMilliseconds(150));
		}

		protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
		{
			base.OnBackKeyPress(e);
			if (_hystory.Count > 0)
			{
				e.Cancel = true;
				// Pop pages from history occur in browser_Navigating method because 
				// there we check navigating to new page or back to previous
				Uri previousPage = _hystory.Peek();
				_currentPage = previousPage;
				browser.Navigate(previousPage.AsLocalHystoryUri());
			}
		}

		private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				Uri outputUri;
				string urlAddress = (sender as TextBox).Text;
				if (urlAddress.IndexOf("http") == -1)
					urlAddress= "http://" + urlAddress;

				if (Uri.TryCreate(urlAddress, UriKind.Absolute, out outputUri) && Uri.IsWellFormedUriString(urlAddress, UriKind.Absolute) && (sender as TextBox).Text.Contains('.'))
				{
					SetCurrentDomainFromUrl(outputUri);
					Navigate(outputUri);
				}
				else
				{
					SetCurrentDomainFromUrl(new Uri("https://www.google.com",UriKind.Absolute));
					Search((sender as TextBox).Text);
				}
			}
		}

		void HandleResponse(IAsyncResult result)
		{
			HttpWebResponse response;
			try
			{
				response = (HttpWebResponse)_request.EndGetResponse(result);
			}
			catch (Exception e)
			{
				Dispatcher.BeginInvoke(() =>
				{
					progressBar.IsIndeterminate = false;
					if (_currentPage.OriginalString != START_PAGE)
						SetCurrentDomainFromUrl(_currentPage);
#if DEBUG
					MessageBox.Show(AppResources.ExceptionMessage+Environment.NewLine+_request.RequestUri.OriginalString+Environment.NewLine+e.Message);
#else
					MessageBox.Show(AppResources.ExceptionMessage);
#endif
				});
				return;
			}

			Stream sourceStream = Utils.CopyAndClose(response.GetResponseStream(), (int)response.ContentLength);
			
			if (response.Headers[HttpRequestHeader.ContentEncoding] == "gzip")
				sourceStream = new GZipStream(sourceStream, CompressionMode.Decompress);

			string fileName = _request.RequestUri.GetLocalHystoryFileName();

			using (IsolatedStorageFileStream savefilestr = new IsolatedStorageFileStream(fileName, FileMode.Create, FileAccess.Write, IsolatedStorageFile.GetUserStoreForApplication()))
			{				
				StreamReader sourceReader = new StreamReader(sourceStream);
				string content = sourceReader.ReadToEnd();
				string charset = null;

				try
				{
					charset = Regex.Match(content, "charset=([^\";']+)").Groups[1].Value;
					if (charset == "")
						content = content.Insert(content.IndexOf("<head>")+6,"<meta content=\"text/html; charset=UTF-8\" http-equiv=\"Content-Type\">");
				}
				catch (System.Exception ex)
				{
					MessageBox.Show("parse charset error");
				}
		
				Encoding encoding = Utils.GetEncodingByString(charset);

				if (encoding != null)
				{
					sourceStream.Position = 0;
					StreamReader r = new StreamReader(sourceStream, encoding);
					content = r.ReadToEnd();
					content = content.Replace(charset, "utf-8");
				}

				RemoveImgTagsFromPage(ref content);

				StreamWriter sw = new StreamWriter(savefilestr);
				sw.Write(content);
				savefilestr.Close();		
			}

			sourceStream.Dispose();

			Dispatcher.BeginInvoke(() =>
			{
				browser.Navigate(new Uri(fileName, UriKind.Relative));
			});
		}

		void RemoveImgTagsFromPage(ref string pageContent)
		{				
			pageContent = Regex.Replace(pageContent, "</?(?i:img)(.|\n)*?>", "");
		}

		/// <summary>
		/// Load page from url to isolated storage and navigate to it
		/// </summary>
		/// <param name="link"></param>
		void Navigate(Uri link)
		{
			this.Focus();
			progressBar.IsIndeterminate = true;
			_request = WebRequest.CreateHttp(link);
			_request.AllowReadStreamBuffering = false; 
			_request.UserAgent = "(compatible; MSIE 10.0; Windows Phone 8.0; Trident/6.0; IEMobile/10.0; ARM; Touch;)";
			// if it file exists in the storage then load it
			if (IsolatedStorageFile.GetUserStoreForApplication().FileExists(link.GetLocalHystoryFileName()))
			{
				//_currentPage = uriForNavigate;
				browser.Navigate(link.AsLocalHystoryUri());
			}
			else
				_request.BeginGetResponse(new AsyncCallback(HandleResponse), null);
		}

		/// <summary>
		/// Search entered words by google
		/// </summary>
		/// <param name="search"></param>
		void Search(string search)
		{
			Uri uriForSearch = new Uri(GOOGLE_SEARCH_DOMAIN + search, UriKind.Absolute);
			Navigate(uriForSearch);
		}

		private void browser_Navigated(object sender, NavigationEventArgs e)
		{
			progressBar.IsIndeterminate = false;
		}

		private void browser_NavigationFailed(object sender, NavigationFailedEventArgs e)
		{
			progressBar.IsIndeterminate = false;
		}

		private void browser_Navigating(object sender, NavigatingEventArgs e)
		{
			if (e.Uri.OriginalString.StartsWith("storagefile") == false)
			{
				e.Cancel = true;
				Uri uriForNavigate = null;
				//Debug.WriteLine(browser.Source);

				if (e.Uri.OriginalString.Contains("http"))
				{
					string clearUri = e.Uri.OriginalString.Remove(0, e.Uri.OriginalString.IndexOf("http"));
					uriForNavigate = new Uri(HttpUtility.UrlDecode(clearUri), UriKind.Absolute);
					if (_currentDomain.Contains("google"))
						uriForNavigate = uriForNavigate.RemoveQueryParams("ei", "sa", "ved", "usg");
					SetCurrentDomainFromUrl(uriForNavigate);
				}
				else
					uriForNavigate = new Uri(HttpUtility.UrlDecode(_currentDomain + "/" + e.Uri.OriginalString), UriKind.Absolute);

				Navigate(uriForNavigate);
			}
			else
			{
				if (_request == null) return;

				if (!_hystory.Contains(_currentPage))
				{
					// if we navigating to new loaded page
					_hystory.Push(_currentPage);
					_currentPage = _request.RequestUri;
				}
				else
				{
					// if we step back by history
					_currentPage = _hystory.Pop();
					if (_currentPage.OriginalString != START_PAGE)
						SetCurrentDomainFromUrl(_currentPage);
					else
						_currentDomain = "";
				}
			}
		}
		
		void SetCurrentDomainFromUrl(Uri navigateUrl)
		{
			_currentDomain = navigateUrl.Scheme + "://" + navigateUrl.Host;
		}

		private void ApplicationBar_StateChanged(object sender, ApplicationBarStateChangedEventArgs e)
		{
			(sender as ApplicationBar).Opacity = e.IsMenuVisible ? 0.85 : 0;
		}

		private void ClearCacheMenuPressed(object sender, EventArgs e)
		{
			long size = IsolatedStorageFile.GetUserStoreForApplication().GetCurretnSize();
			if (MessageBox.Show(AppResources.ClearCacheMessage + " ( " + Utils.ConvertCountBytesToString(size) + " )","",MessageBoxButton.OKCancel) == MessageBoxResult.OK)
			{
				using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
				{
					isf.Remove();
					_hystory.Clear();
					_currentPage = new Uri(START_PAGE, UriKind.Relative);
					browser.Navigate(_currentPage);
				}
			}
		}	

		// Sample code for building a localized ApplicationBar
		private void BuildLocalizedApplicationBar()
		{
		    // Set the page's ApplicationBar to a new instance of ApplicationBar.
		    ApplicationBar = new ApplicationBar();			
			ApplicationBar.Mode = ApplicationBarMode.Minimized;
			ApplicationBar.Opacity = 0;
			ApplicationBar.ForegroundColor = (Color)Application.Current.Resources["PhoneAccentColor"];
			ApplicationBar.StateChanged += ApplicationBar_StateChanged;
		    // Create a new menu item with the localized string from AppResources.
		    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.ClearCacheMenuItem);
			appBarMenuItem.Click += ClearCacheMenuPressed;
		    ApplicationBar.MenuItems.Add(appBarMenuItem);

			appBarMenuItem = new ApplicationBarMenuItem(AppResources.About);
			appBarMenuItem.Click += (object sender, EventArgs e) => { NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative)); };
			ApplicationBar.MenuItems.Add(appBarMenuItem);

			appBarMenuItem = new ApplicationBarMenuItem(AppResources.OpenInIE);
			appBarMenuItem.Click += (object sender, EventArgs e) => 
			{
				if (_currentPage.OriginalString == START_PAGE) return;
				WebBrowserTask task = new WebBrowserTask();
				task.Uri = _currentPage;
				task.Show();
			};
			ApplicationBar.MenuItems.Add(appBarMenuItem);

		}

		private void TextBox_GotFocus(object sender, RoutedEventArgs e)
		{
			ApplicationBar.Opacity = 1;
		}

		private void TextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			ApplicationBar.Opacity = 0;
		}
	}
}