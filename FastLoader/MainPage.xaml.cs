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

		public bool IsEmpty()
		{
			return Domain == String.Empty || Domain == null;
		}
	}

	public partial class MainPage : PhoneApplicationPage
	{
		const string GOOGLE_SEARCH_DOMAIN = "https://www.google.com/search?q=";
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
			_currentPage = new Uri("storagefilestart.html", UriKind.Relative);
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
				Uri previousPage = _hystory.Pop();
				if (_hystory.Count != 1)				
					SetCurrentDomainFromUrl(previousPage);
				
				_currentPage = previousPage;
				browser.Navigate(previousPage.AsLocalHystoryUri());
			}
		}

		private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				Uri outputUri;
				string urlAddress = "http://" + (sender as TextBox).Text;
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
					MessageBox.Show(AppResources.ExceptionMessage);
				});
				return;
			}
			Stream sourceStream = response.GetResponseStream();

			if (response.Headers[HttpRequestHeader.ContentEncoding] == "gzip")
				sourceStream = new GZipStream(sourceStream, CompressionMode.Decompress);

			string _fileName = _currentPage.GetLocalHystoryFileName();

			using (IsolatedStorageFileStream savefilestr = new IsolatedStorageFileStream(_fileName, FileMode.Create, FileAccess.Write, IsolatedStorageFile.GetUserStoreForApplication()))
			{
				sourceStream.CopyTo(savefilestr);
				savefilestr.Close();
			}

			Dispatcher.BeginInvoke(() =>
			{
				Uri uriForNavigate = new Uri(_fileName, UriKind.Relative);
				//MessageBox.Show("Navigating to" + Environment.NewLine + uriForNavigate.OriginalString);
				//_hystory.Push(uriForNavigate);
				browser.Navigate(uriForNavigate);
			});
		}

		void Navigate(Uri link)
		{
			this.Focus();
			//_currentFileName = GetFileNameFromUri(link);
			progressBar.IsIndeterminate = true;

			_hystory.Push(_currentPage);
			_currentPage = link;

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

				if (e.Uri.OriginalString.Contains("http"))
				{
					uriForNavigate = new Uri(e.Uri.OriginalString.Remove(0, e.Uri.OriginalString.IndexOf("http")), UriKind.Absolute);
					if (_currentDomain.Contains("google"))
						uriForNavigate = uriForNavigate.RemoveQueryParams("ei", "sa", "ved", "usg");
					SetCurrentDomainFromUrl(uriForNavigate);
				}
				else
					uriForNavigate = new Uri(_currentDomain + e.Uri.OriginalString, UriKind.Absolute);

				// if it file exists in the storage then load it
				if (IsolatedStorageFile.GetUserStoreForApplication().FileExists(uriForNavigate.GetLocalHystoryFileName()))
				{
					_hystory.Push(_currentPage);
					_currentPage = uriForNavigate;
					browser.Navigate(uriForNavigate.AsLocalHystoryUri());
					return;
				}

				Navigate(uriForNavigate);
			}
		}
		
		void SetCurrentDomainFromUrl(Uri navigateUrl)
		{
			_currentDomain = navigateUrl.Scheme + "://" + navigateUrl.Host;
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