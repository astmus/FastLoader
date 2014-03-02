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

namespace FastLoader
{
	public struct DomainPagesCount
	{
		public string Domain;
		public int CountPages;

		public DomainPagesCount(string domain, int countPages = 0)
		{
			this.Domain = domain;
			this.CountPages = 0;
		}

		public override string ToString()
		{
			return Domain;
		}
	}

	public partial class MainPage : PhoneApplicationPage
	{
		const string GOOGLE_SEARCH_DOMAIN = "https://www.google.com/search?q=";
		// Constructor
		HttpWebRequest _request;
		DomainPagesCount _domain;
		string _currentFileName;
		Stack<Uri> _hystory = new Stack<Uri>();
		Stack<DomainPagesCount> _domains = new Stack<DomainPagesCount>();
		Dictionary<Uri, String> _uriFileNames = new Dictionary<Uri, string>();
		char[] _invalidChars;
		public MainPage()
		{
			InitializeComponent();			
			//browser.NavigateToString("<html><body style=\"background: #000000\"></body></html>");
			Uri start = new Uri("storagefilestart.html", UriKind.Relative);
			_hystory.Push(start);
			browser.Navigate(start);
			_invalidChars = Path.GetInvalidPathChars().Concat(Path.GetInvalidFileNameChars()).ToArray();
			//browser.Navigate(new Uri("http://www.itbox.ua/"));
			// Sample code to localize the ApplicationBar
			//BuildLocalizedApplicationBar();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			Scheduler.Dispatcher.Schedule(() => { LayoutRoot.Children.Remove(placeholder); }, TimeSpan.FromMilliseconds(100));
			//browser.Navigate(new Uri("http://www.fotomag.com.ua", UriKind.Absolute));
		}

		protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
		{
			base.OnBackKeyPress(e);
			if (_hystory.Count > 0)
			{
				e.Cancel = true;
				Uri previousPage = _hystory.Pop();
				if (browser.Source == previousPage)
					previousPage = _hystory.Pop();
				if (_domain.CountPages != 1)
					_domain.CountPages--;
				else
				{
					DomainPagesCount dpc = _domains.Pop();
					if (dpc.Domain == _domain.Domain)
						_domain = _domains.Pop();
					else
						_domain = dpc;
				}

				browser.Navigate(previousPage);
			}
		}

		private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				Uri outputUri;
				string urlAddress = "http://" + (sender as TextBox).Text;
				if (Uri.TryCreate(urlAddress, UriKind.Absolute, out outputUri) && Uri.IsWellFormedUriString(urlAddress,UriKind.Absolute))
				{
					PushDomain(outputUri.OriginalString);
					Navigate(outputUri);
				}
				else
				{
					PushDomain("https://www.google.com");					
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
					_domain.CountPages--;
					MessageBox.Show(AppResources.ExceptionMessage);
				});
				return;
			}
			Stream sourceStream = response.GetResponseStream();

			if (response.Headers[HttpRequestHeader.ContentEncoding] == "gzip")
				sourceStream = new GZipStream(sourceStream, CompressionMode.Decompress);

			using (IsolatedStorageFileStream savefilestr = new IsolatedStorageFileStream(_currentFileName, FileMode.Create, FileAccess.Write, IsolatedStorageFile.GetUserStoreForApplication()))
			{
				sourceStream.CopyTo(savefilestr);
				savefilestr.Close();
			}

			Dispatcher.BeginInvoke(() =>
			{
				Uri uriForNavigate = new Uri(_currentFileName, UriKind.Relative);
				_hystory.Push(uriForNavigate);
				browser.Navigate(uriForNavigate);
			});
		}

		void Navigate(Uri link)
		{
			this.Focus();
			_currentFileName = GetFileNameFromUri(link);
			progressBar.IsIndeterminate = true;
			_domain.CountPages++;
			_request = WebRequest.CreateHttp(link);
			_request.BeginGetResponse(new AsyncCallback(HandleResponse), null);
		}

		void Search(string search)
		{
			//search = HttpUtility.UrlEncode(search);
			Uri uriForSearch = new Uri(GOOGLE_SEARCH_DOMAIN + search, UriKind.Absolute);
			Navigate(uriForSearch);
		}

		private void browser_Navigated(object sender, NavigationEventArgs e)
		{
			progressBar.IsIndeterminate = false;
			
		}

		private void browser_Navigating(object sender, NavigatingEventArgs e)
		{
			if (e.Uri.OriginalString.StartsWith("storagefile") == false)
			{
				e.Cancel = true;
				Uri uriForNavigate = e.Uri;
				//Debug.WriteLine(browser.Source);
				uriForNavigate = new Uri(_domain + e.Uri.OriginalString, UriKind.Absolute);

				if (e.Uri.OriginalString.Contains("http"))
				{
					Uri u = new Uri(e.Uri.OriginalString.Remove(0, e.Uri.OriginalString.IndexOf("http")), UriKind.Absolute);
					PushDomain(u.Scheme + "://" + u.Host);
				}
				// if it file exists in the storage then load it
				if (IsolatedStorageFile.GetUserStoreForApplication().FileExists(GetFileNameFromUri(uriForNavigate)))
				{
					_hystory.Push(uriForNavigate);
					browser.Navigate(uriForNavigate);
					return;
				}

				Navigate(uriForNavigate);
			}
		}

		void PushDomain(string domain)
		{
			DomainPagesCount pc = new DomainPagesCount(domain);
			_domain = pc;
			_domains.Push(pc);
		}

		string GetFileNameFromUri(Uri uri)
		{
			StringBuilder b = new StringBuilder(uri.OriginalString);

			foreach (char c in _invalidChars)
				b.Replace(c, ' ');

			b.Remove(0, 11);
			b.Insert(0, "storagefile");
			b.Replace(" ", "");
			b.Replace(".", "");
			b.Append(".html");
			return b.Length < 150 ? b.ToString() : b.ToString(0,150);
		}

		// Sample code for building a localized ApplicationBar
		//private void BuildLocalizedApplicationBar()
		//{
		//    // Set the page's ApplicationBar to a new instance of ApplicationBar.
		//    ApplicationBar = new ApplicationBar();

		//    // Create a new button and set the text value to the localized string from AppResources.
		//    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
		//    appBarButton.Text = AppResources.AppBarButtonText;
		//    ApplicationBar.Buttons.Add(appBarButton);

		//    // Create a new menu item with the localized string from AppResources.
		//    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
		//    ApplicationBar.MenuItems.Add(appBarMenuItem);
		//}
	}
}