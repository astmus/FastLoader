﻿using System;
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
using FastLoader.Classes;
using FastLoader.Data;
using FastLoader.DB;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using Windows.Storage.Pickers;

namespace FastLoader
{
	public partial class MainPage : PhoneApplicationPage
	{
		const string GOOGLE_SEARCH_DOMAIN = "http://www.google.com/search?btnG=Поиск&gws_rd=ssl&q=";
		
		const string DEFAULT_CONTENT_TYPE = "text/html; charset=UTF-8";
		// Constructor
		HttpWebRequestIndicate _request;
		string _currentDomain;
		//string _currentFileName;
		Uri _currentPage;
		Stack<WebItem> _history = new Stack<WebItem>();
		ObservableCollection<string> _completions = new ObservableCollection<string>();
		bool _nowIsPageRefreshing = false;
		ApplicationBarMenuItem _changeViewItem;
        readonly string _currentOsVersion;
		public MainPage()
		{
			InitializeComponent();
			_currentOsVersion = System.Environment.OSVersion.Version.ToString().Substring(0,3);
			BuildLocalizedApplicationBar();
			PhoneApplicationService.Current.Closing += MainPage_ApplicationClosing;
			PhoneApplicationService.Current.Activated += Current_Activated;
			PhoneApplicationService.Current.Deactivated += Current_Deactivated;
			AppSettings.Instance.SaveAutoCompletionsListValueCahnged += Instance_SaveAutoCompletionsListValueCahnged;
			_currentPage = WebItem.StartPage;
			History.CacheCleared += SettingsPage_ClearCachePressed;
            progressBar.Maximum = 100;
            progressBar.Minimum = 0;
			browser.Navigate(_currentPage);				
		}

		void SettingsPage_ClearCachePressed()
		{			
			_history.Clear();			
			_currentPage = WebItem.StartPage;
			_completions.Clear();
			_request = null;
			browser.Navigate(_currentPage);
		}

		void Current_Deactivated(object sender, DeactivatedEventArgs e)
		{
			if (_request != null && _request.IsPerformed)
				_request.Abort();
		}

		void Current_Activated(object sender, ActivatedEventArgs e)
		{
			if (_request != null && _request.IsAborted)
				Navigate(_request.HttpRequest.RequestUri as WebItem);
		}

		void Instance_SaveAutoCompletionsListValueCahnged(bool obj)
		{
			if (obj == false)
				searchField.ItemsSource = null;
			else
				searchField.ItemsSource = _completions;
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

			if (e.NavigationMode != NavigationMode.New)
				return;
			//load completions
			using (IsolatedStorageFileStream file = IsolatedStorageFile.GetUserStoreForApplication().OpenFile("completions", FileMode.OpenOrCreate, FileAccess.Read))
			{
				StreamReader reader = new StreamReader(file);
				while (!reader.EndOfStream)
					_completions.Add(reader.ReadLine());
				reader.Close();
				file.Close();
			}

			if (AppSettings.Instance.SaveAutocompletionsList)
				searchField.ItemsSource = _completions;
		}

		protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
		{
			base.OnBackKeyPress(e);
			if (_request != null && _request.IsPerformed)
			{				
				_request.Abort();				
				progressBar.IsIndeterminate = false;
				e.Cancel = true;
				return;
			}

			if (_history.Count > 0)
			{
				e.Cancel = true;
				// Pop pages from history occur in browser_Navigating method because 
				// there we check navigating to new page or back to previous
				WebItem previousPage = _history.Peek();
				_currentPage = previousPage;

				browser.Navigate(previousPage.LocalHystoryUri);
			}			
		}

		private void TextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				Uri outputUri;
				string urlAddress = (sender as AutoCompleteBox).Text;

				if (AppSettings.Instance.SaveAutocompletionsList && _completions.Contains(urlAddress) == false)
					_completions.Insert(0, urlAddress);
                                
                if (urlAddress.IndexOf("http") == -1)
					urlAddress = "http://" + urlAddress;
                

				if (Uri.TryCreate(urlAddress, UriKind.Absolute, out outputUri) && Uri.IsWellFormedUriString(urlAddress, UriKind.Absolute) && (sender as AutoCompleteBox).Text.Contains('.'))
				{
					SetCurrentDomainFromUrl(outputUri);
					Navigate(new WebItem(outputUri));
				}
				else
				{
					SetCurrentDomainFromUrl(WebItem.GooglePage);
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
			catch (WebException e)
			{
				WebException ex = e as WebException;
				if (ex.Status == WebExceptionStatus.RequestCanceled)
					return;

				Dispatcher.BeginInvoke(() =>
				{
					progressBar.IsIndeterminate = false;
					if (_currentPage != WebItem.StartPage)
						SetCurrentDomainFromUrl(_currentPage);
#if DEBUG
					MessageBox.Show(AppResources.ExceptionMessage + Environment.NewLine + _request.HttpRequest.RequestUri.OriginalString + Environment.NewLine + e.Message);
#else
					MessageBox.Show(AppResources.ExceptionMessage);
#endif
				});
				return;
			}
            string charsetFromHeaders = GetCharsetFromHeaders(response);
            if (!string.IsNullOrEmpty(charsetFromHeaders) && !charsetFromHeaders.Contains("text"))
            {                
                WebBrowserTask task = new WebBrowserTask();
                task.Uri = _request.HttpRequest.RequestUri;
                Dispatcher.BeginInvoke(() =>
                {
                    progressBar.IsIndeterminate = false;
                });
                _request = null;
                task.Show();
                return;
            }
                
			Stream temporaryStream;
            MemoryStream sourceStream = Utils.CopyAndClose(response.GetResponseStream(), (int)response.ContentLength, ProgressBarChanged, OnLoadCompleted);			

			bool isZipped = response.Headers[HttpRequestHeader.ContentEncoding] == "gzip";
			
			if (isZipped)
				temporaryStream = new GZipStream(sourceStream, CompressionMode.Decompress);
			else
				temporaryStream = sourceStream;
			

			//string fileName = _request.HttpRequest.RequestUri.GetLocalHystoryFileName();
			_request.IsPerformed = false;
			StreamReader reader = new StreamReader(temporaryStream);
			string content = reader.ReadToEnd();
			string charset = null;
			bool charsetContainsInPage = true;
			try
			{
				//handle encoding
				charset = GetCharsetFromContent(content);
				if (string.IsNullOrEmpty(charset))
				{
					charsetContainsInPage = false;
					charset = charsetFromHeaders;
                    if (charset.Contains("="))
                        charset = charset.Substring(charset.IndexOf("=") + 1);
				}
			}
			catch (System.Exception ex)
			{
				MessageBox.Show("parse charset error");
			}

			Encoding encoding = Utils.GetEncodingByString(charset);

			if (encoding != null)
			{
				try
				{
					sourceStream.Position = 0;
					StreamReader r = new StreamReader(isZipped ? (new GZipStream(sourceStream, CompressionMode.Decompress)) as Stream : sourceStream, encoding);
					content = r.ReadToEnd();
					if (charsetContainsInPage)
						content = content.Replace(charset, "utf-8");
				}
				catch (System.Exception ex)
				{
					Dispatcher.BeginInvoke(() =>
					{
						progressBar.IsIndeterminate = false;
						if (_currentPage != WebItem.StartPage)
							SetCurrentDomainFromUrl(_currentPage);
#if DEBUG
						MessageBox.Show(AppResources.ExceptionMessage + Environment.NewLine + _request.HttpRequest.RequestUri.OriginalString + Environment.NewLine + ex.Message);
#else
						MessageBox.Show(AppResources.ExceptionMessage);
#endif
					});
					return;
				}
			}

			if (charsetContainsInPage == false)
			{
                int pos = content.IndexOf("</head>");
				if (pos == -1)
					pos = content.IndexOf("</HEAD>");
				if (pos != -1)
					content = content.Insert(pos , string.Format("<meta content=\"{0}\" http-equiv=\"Content-Type\">", DEFAULT_CONTENT_TYPE));
			}			
            
			RemoveImgTagsFromPage(ref content);
			WebItem item = _request.HttpRequest.RequestUri as WebItem;

			AddCacheItemToStorage(content);

			sourceStream.Dispose();

			Dispatcher.BeginInvoke(() =>
			{
				browser.Navigate(item.LocalHystoryUri);
			});
		}
        
        private void OnLoadCompleted()
        {
            Dispatcher.BeginInvoke(() => 
            {
                progressBar.Value = 0;
            });
        }

        private void ProgressBarChanged(uint pertents)
        {
            Dispatcher.BeginInvoke(() => 
            {
                progressBar.IsIndeterminate = false;
                progressBar.Value = pertents;
            });
        }

		string GetCharsetFromContent(string content)
		{
			//return Regex.Match(content, "<meta.+?charset=([^\";']+)").Groups[1].Value;            
            return Regex.Match(content, "(?i)<meta.+?charset=\"?(.+?)[\";']").Groups[1].Value;
		}

		string GetCharsetFromHeaders(HttpWebResponse response)
		{
			return response.Headers[HttpRequestHeader.ContentType];
		}

		void RemoveImgTagsFromPage(ref string pageContent)
		{
			pageContent = Regex.Replace(pageContent, "</?(?i:img)(.|\n)*?>", "");
		}

		/// <summary>
		/// Load page from url to isolated storage and navigate to it
		/// </summary>
		/// <param name="link"></param>
		public void Navigate(Uri link)
		{
			this.Focus();
			progressBar.IsIndeterminate = true;
			WebItem navItem = link is WebItem ? link as WebItem : new WebItem(link);            

            _request = new HttpWebRequestIndicate(WebRequest.CreateHttp(navItem));
			// if it file exists in the storage then load it
			if (IsolatedStorageFile.GetUserStoreForApplication().FileExists(navItem.LocalHystoryFileName))
				//_currentPage = uriForNavigate;
				browser.Navigate(navItem.LocalHystoryUri);
			else
			{				
				_request.HttpRequest.AllowReadStreamBuffering = false;                
                _request.HttpRequest.UserAgent = "(compatible; MSIE 10.0; Windows Phone " + _currentOsVersion + "; Trident/6.0; IEMobile/10.0; ARM; Touch;)";
				_request.BeginGetResponse(new AsyncCallback(HandleResponse), null);
			}
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
			{
				Scheduler.Dispatcher.Schedule(() =>
				{
					LayoutRoot.Children.Remove(placeholder);
					ApplicationBar.IsVisible = true;
				}, TimeSpan.FromMilliseconds(500));
				isFirstTime = false;
			}
			
			AddHistoryItemToStorage();
			progressBar.IsIndeterminate = false;
		}

		private void browser_NavigationFailed(object sender, NavigationFailedEventArgs e)
		{
			progressBar.IsIndeterminate = false;
		}

		private void browser_Navigating(object sender, NavigatingEventArgs e)
		{
            progressBar.IsIndeterminate = true;
			if (e.Uri.OriginalString.StartsWith("storagefile") == false)
			{
				e.Cancel = true;
				WebItem uriForNavigate = null;
				
				if (e.Uri.OriginalString.Contains("http"))
				{
					uriForNavigate = GetUriForNavigate(e.Uri.OriginalString);
					if (uriForNavigate == null) return;					

					List<string> forRemoveParams = new List<string>();
					if (uriForNavigate.OriginalString.Contains("ei")) 
						forRemoveParams.Add("ei");
					if (uriForNavigate.OriginalString.Contains("sa"))
						forRemoveParams.Add("sa");
					if (uriForNavigate.OriginalString.Contains("ved"))
						forRemoveParams.Add("ved");
					if (uriForNavigate.OriginalString.Contains("usg"))
						forRemoveParams.Add("usg");

					if (forRemoveParams.Count > 0)
						uriForNavigate = uriForNavigate.RemoveQueryParams(forRemoveParams.ToArray());
					SetCurrentDomainFromUrl(uriForNavigate);
				}
				else
				{
					string ump = e.Uri.OriginalString[0] != '/' ? "/" : "" ;
					string s = _currentDomain + ump + e.Uri.OriginalString;
					uriForNavigate = new WebItem(HttpUtility.UrlDecode(s), 0);
				}

				Navigate(uriForNavigate);
			}
			else
			{				
				if (_request == null || _nowIsPageRefreshing)
				{
					_nowIsPageRefreshing = false;
					return;
				}                
				if (!_history.Contains(_currentPage))
					InsertToHystory(e);
				else
					PullOutFromHystory();
			}
		}

		private void PullOutFromHystory()
		{
			// if we step back by history
			_currentPage = _history.Pop();
			if (_currentPage != WebItem.StartPage)
				SetCurrentDomainFromUrl(_currentPage);
			else
				_currentDomain = "";
		}
		private void InsertToHystory(NavigatingEventArgs e)
		{
			WebItem navItem = _request.HttpRequest.RequestUri as WebItem;

			if (_currentPage != navItem)
			{
				// if we navigating to new loaded page
				_history.Push(_currentPage as WebItem);
				_currentPage = navItem;
			}
			else
			{
				//if redirection
				e.Cancel = true;
				progressBar.IsIndeterminate = false;
			}
		}

		WebItem GetUriForNavigate(string originalString)
		{
			string clearUri = originalString.Remove(0, originalString.IndexOf("http"));
			try
			{
				return new WebItem(HttpUtility.UrlDecode(clearUri), 0);
			}
			catch
			{
				MessageBox.Show(AppResources.ExceptionMessage);
				return null;
			}
		}

		void AddHistoryItemToStorage()
		{
			if (_request == null || _currentPage == WebItem.StartPage) return;
			WebItem current = _currentPage as WebItem;
			var tmp = FSDBManager.Instance.Cache.Where(cach => cach.Link == current.OriginalString).FirstOrDefault();
			HistoryItem hitem = new HistoryItem()
			{
				OpenTime = DateTime.Now,
				Link = current.OriginalString,
				Title = (tmp != null) ? tmp.Title : ""
			};

			FSDBManager.Instance.History.InsertOnSubmit(hitem);
			FSDBManager.Instance.SubmitChanges();

		}

		void AddCacheItemToStorage(string content)
		{
			WebItem item = _request.HttpRequest.RequestUri as WebItem;

			using (IsolatedStorageFileStream savefilestr = new IsolatedStorageFileStream(item.LocalHystoryFileName, FileMode.Create, FileAccess.Write, IsolatedStorageFile.GetUserStoreForApplication()))
			{
				StreamWriter sw = new StreamWriter(savefilestr);
				sw.Write(content);
				item.Size = savefilestr.Length;
				savefilestr.Close();
			}

			//add item to persist history
			CachedItem cachedItem = new CachedItem()
			{
				Link = item.OriginalString,
				Size = item.Size,
				OpenTime = DateTime.Now,
				Title = Utils.GetTitleFromHtmlPage(content),
				FormatedSize = Utils.ConvertCountBytesToString(item.Size)
			};

			FSDBManager.Instance.Cache.InsertOnSubmit(cachedItem);
			FSDBManager.Instance.SubmitChanges();
		}

		void SetCurrentDomainFromUrl(Uri navigateUrl)
		{
			_currentDomain = navigateUrl.Scheme + "://" + navigateUrl.Host;
		}

		private void ApplicationBar_StateChanged(object sender, ApplicationBarStateChangedEventArgs e)
		{
			(sender as ApplicationBar).Opacity = e.IsMenuVisible ? 0.95 : 0;
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
			ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.Refresh);
			appBarMenuItem.Click += RefreshCurrentPage;
			ApplicationBar.MenuItems.Add(appBarMenuItem);

			appBarMenuItem = new ApplicationBarMenuItem(AppResources.OpenInIE);
			appBarMenuItem.Click += (object sender, EventArgs e) =>
			{
                OpenCurrentPageInIE();
			};
			ApplicationBar.MenuItems.Add(appBarMenuItem);
			
			appBarMenuItem = new ApplicationBarMenuItem(AppResources.History);			
			appBarMenuItem.Click += OpenHistoryMenuItem_Click;
			ApplicationBar.MenuItems.Add(appBarMenuItem);
			 
			appBarMenuItem = new ApplicationBarMenuItem(AppResources.Settings);
			appBarMenuItem.Click += (object sender, EventArgs e) => { NavigationService.Navigate(new Uri("/Settings.xaml", UriKind.Relative)); };
			ApplicationBar.MenuItems.Add(appBarMenuItem);

			_changeViewItem = new ApplicationBarMenuItem(AppResources.LandscapeMode);
			_changeViewItem.Click += ChangeViewMode;
			ApplicationBar.MenuItems.Add(_changeViewItem);
		}

        private void OpenCurrentPageInIE()
        {
            if (_currentPage == WebItem.StartPage) return;
            WebBrowserTask task = new WebBrowserTask();
            task.Uri = _currentPage;
            task.Show();
        }

		bool _isPortrait = true;
		void ChangeViewMode(object sender, EventArgs e)
		{
			_isPortrait = !_isPortrait;
			if (_isPortrait)
			{
				this.SupportedOrientations = SupportedPageOrientation.Portrait;
				this.Orientation = PageOrientation.PortraitUp;
				_changeViewItem.Text = AppResources.LandscapeMode;
			}
			else
			{
				this.SupportedOrientations = SupportedPageOrientation.Landscape;
				this.Orientation = PageOrientation.Landscape;
				_changeViewItem.Text = AppResources.PortraitMode;
			}
		}

		void OpenHistoryMenuItem_Click(object sender, EventArgs e)
		{
			NavigationService.Navigate(new Uri("/History.xaml", UriKind.Relative)); 
		}

		void NoticeAboutBadPage(object sender, EventArgs e)
		{
			if (_currentPage == WebItem.StartPage)
				return;

			EmailComposeTask email = new EmailComposeTask();
			email.Body = AppResources.BadPageMessage + Environment.NewLine + _currentPage.OriginalString;
			email.To = "astmus@live.com";
			email.Subject = "fast loader report";
			email.Show();
		}

		void RefreshCurrentPage(object sender, EventArgs e)
		{
			string filename = (_currentPage as WebItem).LocalHystoryFileName;
			if (IsolatedStorageFile.GetUserStoreForApplication().FileExists(filename))
				IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(filename);

			_nowIsPageRefreshing = true;
			Navigate(_currentPage);
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