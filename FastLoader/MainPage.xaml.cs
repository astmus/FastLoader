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
using System.Collections.ObjectModel;

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
		const string DEFAULT_CONTENT_TYPE = "text/html; charset=UTF-8"; 
		// Constructor
		HttpWebRequest _request;
		string _currentDomain;
		//string _currentFileName;
		Uri _currentPage;
		Stack<Uri> _hystory = new Stack<Uri>();
		ObservableCollection<string> _completions = new ObservableCollection<string>();
		//Stack<DomainPagesCount> _domains = new Stack<DomainPagesCount>();
		Dictionary<Uri, String> _uriFileNames = new Dictionary<Uri, string>();
		public MainPage()
		{
			InitializeComponent();
			BuildLocalizedApplicationBar();
			_currentPage = new Uri(START_PAGE, UriKind.Relative);
			browser.Navigate(_currentPage);
			(App.Current as App).ApplicationClosing += MainPage_ApplicationClosing;
		}

		void MainPage_ApplicationClosing(object sender, ClosingEventArgs e)
		{
			using (IsolatedStorageFileStream file = IsolatedStorageFile.GetUserStoreForApplication().OpenFile("completions", FileMode.Create, FileAccess.Write))
			{
				StreamWriter writer = new StreamWriter(file);
				foreach (string item in _completions)
					writer.WriteLine(item);
				writer.Close();
				file.Close();
			}
		}	

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			//load completions
			using (IsolatedStorageFileStream file = IsolatedStorageFile.GetUserStoreForApplication().OpenFile("completions",FileMode.OpenOrCreate,FileAccess.Read))
			{
				StreamReader reader = new StreamReader(file);
				while (!reader.EndOfStream)
					_completions.Add(reader.ReadLine());
			}
			searchField.ItemsSource = _completions;
			
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

		private void TextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				Uri outputUri;
				string urlAddress = (sender as AutoCompleteBox).Text;
				if (_completions.Contains(urlAddress) == false)
					_completions.Insert(0,urlAddress);

				if (urlAddress.IndexOf("http") == -1)
					urlAddress= "http://" + urlAddress;

				if (Uri.TryCreate(urlAddress, UriKind.Absolute, out outputUri) && Uri.IsWellFormedUriString(urlAddress, UriKind.Absolute) && (sender as AutoCompleteBox).Text.Contains('.'))
				{
					SetCurrentDomainFromUrl(outputUri);
					Navigate(outputUri);
				}
				else
				{
					SetCurrentDomainFromUrl(new Uri("https://www.google.com",UriKind.Absolute));
					Search((sender as AutoCompleteBox).Text);
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
				bool charsetContainsInPage = true;
				try
				{
					//handle encoding
					charset = GetCharsetFromContent(content);
					if (charset == "")
					{
						charsetContainsInPage = false;
						charset = GetCharsetFromHeaders(response);
					}
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
					if (charsetContainsInPage)
						content = content.Replace(charset, "utf-8");
				}

				if (charsetContainsInPage == false)
				{
					int pos = content.IndexOf("<head>");
					if (pos != -1)
						content = content.Insert(pos + 6, string.Format("<meta content=\"{0}\" http-equiv=\"Content-Type\">", DEFAULT_CONTENT_TYPE));
				}

				if (_currentDomain.Contains("google"))
					content = Regex.Replace(content, "<form action=\"/search.*form>","");

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

		string GetCharsetFromContent(string content)
		{
			return Regex.Match(content, "<meta.+?charset=([^\";']+)").Groups[1].Value;			
		}

		string GetCharsetFromHeaders(HttpWebResponse response)
		{
			string contentType = response.Headers[HttpRequestHeader.ContentType];
			return Regex.Match(contentType, "charset=([^\";']+)").Groups[1].Value;			
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

		static bool isFirstTime = true;
		private void browser_Navigated(object sender, NavigationEventArgs e)
		{
			if (isFirstTime)
				Scheduler.Dispatcher.Schedule(() => 
				{ 
					LayoutRoot.Children.Remove(placeholder);
					ApplicationBar.IsVisible = true;
				}, TimeSpan.FromMilliseconds(150));
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
			ApplicationBar.IsVisible = false;
			ApplicationBar.Mode = ApplicationBarMode.Minimized;
			ApplicationBar.Opacity = 0;
			ApplicationBar.ForegroundColor = (Color)Application.Current.Resources["PhoneAccentColor"];
			ApplicationBar.StateChanged += ApplicationBar_StateChanged;
		    // Create a new menu item with the localized string from AppResources.
		    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.ClearCacheMenuItem);
			appBarMenuItem.Click += ClearCacheMenuPressed;
		    ApplicationBar.MenuItems.Add(appBarMenuItem);

			appBarMenuItem = new ApplicationBarMenuItem(AppResources.Refresh);
			appBarMenuItem.Click += RefreshCurrentPage;
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

			appBarMenuItem = new ApplicationBarMenuItem(AppResources.About);
			appBarMenuItem.Click += (object sender, EventArgs e) => { NavigationService.Navigate(new Uri("/AboutPage.xaml", UriKind.Relative)); };
			ApplicationBar.MenuItems.Add(appBarMenuItem);
		}

		void RefreshCurrentPage(object sender, EventArgs e)
		{
			string filename = _currentPage.GetLocalHystoryFileName();
			if (IsolatedStorageFile.GetUserStoreForApplication().FileExists(filename))
			{
				IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(filename);
				Navigate(_currentPage);
			}
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