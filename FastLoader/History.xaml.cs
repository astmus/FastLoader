using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using FastLoader.DB;
using System.Diagnostics;
using FastLoader.Data;
using FastLoader.Interfaces;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO.IsolatedStorage;
using FastLoader.Classes;
using FastLoader.Resources;
using FastLoader.Extensions;
using Windows.Phone.System;
using System.IO;
using System.Threading.Tasks;

namespace FastLoader
{
	public partial class History : PhoneApplicationPage
	{
		IWebItem _item;
		ApplicationBarIconButton select;
		ApplicationBarIconButton delete;
		ApplicationBarIconButton deleteAll;
		LongListMultiSelector _currentList;
		public static event Action CacheCleared;
		public History()
		{
			InitializeComponent();
			AppBar();
		}

		private void SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_currentList != null)
				_currentList.IsSelectionEnabled = false;
			switch (MainPivot.SelectedIndex)
			{
				case 0:
					if (cache.ItemsSource == null)
						InitItems<CachedItem>(cache);
					_currentList = cache;
					break;
				case 1:
					if (history.ItemsSource == null)
						InitItems<HistoryItem>(history);
					_currentList = history;
					break;
			}				
		}

		async void InitItems<T>(LongListMultiSelector list) where T : class, IWebItem
		{						
			progressBar.IsIndeterminate = true;
			progressBar.Visibility = Visibility.Visible;	
			list.ItemsSource = await FSDBManager.Instance.GetSortedItems<T>();
			progressBar.IsIndeterminate = false;
			progressBar.Visibility = Visibility.Collapsed;
		}

		void AppBar()
		{
			select = new ApplicationBarIconButton();
			select.IconUri = new Uri("Assets/ApplicationBar.Select.png", UriKind.RelativeOrAbsolute);
			select.Text = "select";
			select.Click += OnSelectClick;

			delete = new ApplicationBarIconButton();
			delete.IconUri = new Uri("Assets/ApplicationBar.Delete.png", UriKind.RelativeOrAbsolute);
			delete.Text = "delete";
			delete.Click += OnDeleteClick;
			ApplicationBar.Buttons.Add(select);
			deleteAll = new ApplicationBarIconButton();
			deleteAll.IconUri = new Uri("Assets/cancel.png", UriKind.RelativeOrAbsolute);
			deleteAll.Text = "delete all";
			deleteAll.Click += deleteAll_Click;
			ApplicationBar.Buttons.Add(deleteAll);
		}

		void deleteAll_Click(object sender, EventArgs e)
		{
			if (_currentList == cache)
				ClearCache();
			else
				ClearHistory();
		}

		void ClearCache()
		{
			FSDBManager.Instance.Dispose();
			FSDBManager.Instance = null;
			long size = (FSDBManager.Instance.Cache.Count() > 0) ? FSDBManager.Instance.Cache.Sum(item => item.Size) : 0;
			if (MessageBox.Show(AppResources.ClearCacheMessage + " ( " + Utils.ConvertCountBytesToString(size) + " )", "", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
			{
				using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
				{
					List<string> files = isf.GetAllFiles();					
					foreach (string path in files)
						if (!path.Contains(".sdf") && !path.Contains("__ApplicationSettings") && !path.Contains(".tmp"))
							isf.DeleteFile(path);
				}				
				FSDBManager.Instance.Cache.DeleteAllOnSubmit(FSDBManager.Instance.Cache);
				FSDBManager.Instance.SubmitChanges();
				cache.ItemsSource = null;
				if (CacheCleared != null)
					CacheCleared();
			}			
		}

		void ClearHistory()
		{
			if (MessageBox.Show(AppResources.ClearHistory, "", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
			{
				FSDBManager.Instance.History.DeleteAllOnSubmit(FSDBManager.Instance.History);
				FSDBManager.Instance.SubmitChanges();
				history.ItemsSource = null;
				if (CacheCleared != null)
					CacheCleared();
			}
		}

		private void SetupEmailApplicationBar()
		{
			ClearApplicationBar();

			if (_currentList.IsSelectionEnabled)
			{
				ApplicationBar.Buttons.Add(delete);
				UpdateApplicationBar();
			}
			else
			{
				ApplicationBar.Buttons.Add(select);
				ApplicationBar.Buttons.Add(deleteAll);
			}
			ApplicationBar.IsVisible = true;
		}

		private void UpdateApplicationBar()
		{
			if (_currentList.IsSelectionEnabled)
			{
				bool hasSelection = ((_currentList.SelectedItems != null) && (_currentList.SelectedItems.Count > 0));
				delete.IsEnabled = hasSelection;				
			}
		}

		void ClearApplicationBar()
		{
			while (ApplicationBar.Buttons.Count > 0)
			{
				ApplicationBar.Buttons.RemoveAt(0);
			}
		}

		void OnDeleteClick(object sender, EventArgs e)
		{
			for (int i = 0; i < _currentList.SelectedItems.Count; i++)
			{
				IWebItem item = _currentList.SelectedItems[i] as IWebItem;
				if (item is CachedItem)
				{
					CachedItem cacheItem = item as CachedItem;
					DeleteItem<CachedItem>(cacheItem);
					FSDBManager.Instance.Cache.DeleteOnSubmit(cacheItem);
					string isoFileName = WebItem.LocalHystoryFileNameFromUrlString(cacheItem.Link);
					IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(isoFileName);
				}
				else
				{
					DeleteItem<HistoryItem>(item as HistoryItem);
					FSDBManager.Instance.History.DeleteOnSubmit(item as HistoryItem);
				}
				FSDBManager.Instance.SubmitChanges();
			}
			_currentList.IsSelectionEnabled = false;
		}

		void DeleteItem<T>(T item) where T :class, IWebItem
		{
			var groups = _currentList.ItemsSource as ObservableCollection<ItemsGroup<T>>;
			ItemsGroup<T> group = groups.Where(g => g.Key == item.OpenTime.Date.ToString("dd MMMM yyyy")).FirstOrDefault() as ItemsGroup<T>;
			group.Remove(item);			
		}

		void OnSelectClick(object sender, EventArgs e)
		{
			_currentList.IsSelectionEnabled = true;
		}

		private void Grid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			_item = (sender as FrameworkElement).DataContext as IWebItem;
			NavigationService.GoBack();
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			base.OnNavigatedFrom(e);
			if (_item != null)
				(e.Content as MainPage).Navigate(new Uri(_item.Link,UriKind.Absolute));			

		}

		private void items_IsSelectionEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			SetupEmailApplicationBar();
		}

		private void items_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_currentList.IsSelectionEnabled)
			{
				bool hasSelection = ((_currentList.SelectedItems != null) && (_currentList.SelectedItems.Count > 0));
				delete.IsEnabled = hasSelection;
			}
		}
	}
}