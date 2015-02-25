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
using System.Threading;
using System.Windows.Threading;
using Microsoft.Phone.Reactive;

namespace FastLoader
{
	public partial class History : PhoneApplicationPage
	{
		IWebItem _item;
		ApplicationBarIconButton select;
		ApplicationBarIconButton delete;
		ApplicationBarIconButton deleteAll;
		ApplicationBarIconButton search;
		LongListMultiSelector _currentList;
		string _historySearchKeyWord = String.Empty;
		string _cacheSearchKeyWord = String.Empty;
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
                    _currentList = todayItems;
                    if (todayItems.ItemsSource == null)
                        InitItems<CachedItem>(todayItems, true);
                    if (searchBox.Text != _cacheSearchKeyWord)
                    {
                        ScheduleSearchItems(searchBox.Text, 100);
                        _cacheSearchKeyWord = searchBox.Text;
                    }
                    break;
				case 1:
					_currentList = cache;	
					if (cache.ItemsSource == null)
						InitItems<CachedItem>(cache);
					if (searchBox.Text != _cacheSearchKeyWord)
					{
						ScheduleSearchItems(searchBox.Text, 100);
						_cacheSearchKeyWord = searchBox.Text;
					}									
					break;
				case 2:
					_currentList = history;
					if (history.ItemsSource == null)
						InitItems<HistoryItem>(history);
					if (searchBox.Text != _historySearchKeyWord)
					{
						ScheduleSearchItems(searchBox.Text, 100);
						_historySearchKeyWord = searchBox.Text;
					}					
					break;				
			}			
		}

		async void InitItems<T>(LongListMultiSelector list) where T : class, IWebItem
		{
			StartDisplayLoading();			
			list.ItemsSource = await FSDBManager.Instance.GetSortedItems<T>();
			EndDisplayLoading();
		}

        async void InitItems<T>(LongListMultiSelector list, bool isToday) where T : class, IWebItem
        {
            StartDisplayLoading();
            list.ItemsSource = await FSDBManager.Instance.GetTodaySortedItems<T>();
            EndDisplayLoading();
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
			
			deleteAll = new ApplicationBarIconButton();
			deleteAll.IconUri = new Uri("Assets/cancel.png", UriKind.RelativeOrAbsolute);
			deleteAll.Text = "delete all";
			deleteAll.Click += deleteAll_Click;

			search = new ApplicationBarIconButton();
			search.IconUri = new Uri("Assets/search.png", UriKind.RelativeOrAbsolute);
			search.Text = "search";
			search.Click += search_Click;

			ApplicationBar.Buttons.Add(select);
			ApplicationBar.Buttons.Add(deleteAll);
			ApplicationBar.Buttons.Add(search);
		}

		void search_Click(object sender, EventArgs e)
		{
			if (searchBox.Height != 0)
			{
				searchHeightAnimationHide.Begin();
				this.Focus();
			}
			else
				searchHeightAnimationShow.Begin();	
		}

		private void ShowSearchBoxAnimation_Completed(object sender, EventArgs e)
		{
			searchBox.Focus();
		}		

		private void searchBox_LostFocus(object sender, RoutedEventArgs e)
		{
			searchHeightAnimationHide.Begin();
		}

		void deleteAll_Click(object sender, EventArgs e)
		{
			if (searchBox.Text.Length == 0)
			{
				//if search box is empty then we remove all items from cache or history
				if (_currentList == cache || _currentList == todayItems)
					ClearCache();
				else
					ClearHistory();
			}
			else
			{	
				// if search box has value then we remove only filtered  items
				if (MessageBox.Show(AppResources.RemoveAllItemsFromTheList, "", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
					DeleteItemsFromCurrentList();
			}			
		}

		void ClearCache()
		{
			FSDBManager.Instance.Dispose();			
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
				//if (CacheCleared != null)
				//	CacheCleared();
			}
		}

		private void SetupDeletengApplicationBar()
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
				ApplicationBar.Buttons.Add(search);
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
				ApplicationBar.Buttons.RemoveAt(0);
		}

		void OnDeleteClick(object sender, EventArgs e)
		{
			DeleteItemsFromCurrentList(_currentList.SelectedItems);
			_currentList.IsSelectionEnabled = false;
		}

		void DeleteItemsFromCurrentList(IList selecteditems = null)
		{
			IList items = null;

			Action<IWebItem> delete;
			if (_currentList.ItemsSource is ObservableCollection<ItemsGroup<CachedItem>>)
			{
				delete = DeleteCachedItem;
				items = GetItemsForDelete<CachedItem>(selecteditems);
			}
			else
			{
				delete = DeleteHistoryItem;
				items = GetItemsForDelete<HistoryItem>(selecteditems);
			}			
			
			for (int i = 0; i < items.Count; i++)
				delete(items[i] as IWebItem);				
			
			FSDBManager.Instance.SubmitChanges();
		}

		IList GetItemsForDelete<T>(IList selectedItems)
		{
			if (selectedItems != null)
				return selectedItems;
			else
				return (_currentList.ItemsSource as ObservableCollection<ItemsGroup<T>>).SelectMany(sm => sm.ToList()).ToList();
		}

		private void DeleteHistoryItem(IWebItem item)
		{
			DeleteItemFromMultiSelectList<HistoryItem>(item as HistoryItem);
			FSDBManager.Instance.History.DeleteOnSubmit(item as HistoryItem);			
		}
		private void DeleteCachedItem(IWebItem item)
		{
			CachedItem cacheItem = item as CachedItem;
			DeleteItemFromMultiSelectList<CachedItem>(cacheItem);
			FSDBManager.Instance.Cache.DeleteOnSubmit(cacheItem);
			string isoFileName = WebItem.LocalHystoryFileNameFromUrlString(cacheItem.Link);
			IsolatedStorageFile.GetUserStoreForApplication().DeleteFile(isoFileName);			
		}
		//void DeleteSearchedItems()
		//{
		//	if (MessageBox.Show(AppResources.DeleteFoundItems, "", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
		//	{
		//		var itemsCollection = search.ItemsSource as ObservableCollection<ItemsGroup<CachedItem>>;
		//		List<CachedItem> items = itemsCollection.SelectMany(e => e.ToList()).ToList();

		//		using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication())
		//		{
		//			List<string> files = items.Select(s=> WebItem.LocalHystoryFileNameFromUrlString(s.Link)).ToList();
		//			foreach (string path in files)				
		//					isf.DeleteFile(path);
		//		}

		//		FSDBManager.Instance.Cache.DeleteAllOnSubmit(items);
		//		FSDBManager.Instance.SubmitChanges();
		//		search.ItemsSource = null;

		//		var cachedItemsCollection = cache.ItemsSource as ObservableCollection<ItemsGroup<CachedItem>>;

		//		foreach (ItemsGroup<CachedItem> g in itemsCollection)
		//		{
		//			var currentGroup = cachedItemsCollection.Where(w => w.Key == g.Key).FirstOrDefault();
		//			if (currentGroup == null) continue;
		//			foreach (CachedItem item in g)
		//			{
						
		//			}
		//		}
		//	}
		//}

		void DeleteItemFromMultiSelectList<T>(T item) where T : IWebItem
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
			SetupDeletengApplicationBar();
		}

		private void items_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_currentList.IsSelectionEnabled)
			{
				bool hasSelection = ((_currentList.SelectedItems != null) && (_currentList.SelectedItems.Count > 0));
				delete.IsEnabled = hasSelection;
			}
		}

		private void cache_ItemRealized(object sender, ItemRealizationEventArgs e)
		{
			int i = 0;
			var s = e.ItemKind;
		}
		
		private void searchBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
		{
			ScheduleSearchItems((sender as TextBox).Text,1200);
			if (MainPivot.SelectedIndex == 0)
				_cacheSearchKeyWord = searchBox.Text;
			else
				_historySearchKeyWord = searchBox.Text;
		}

		Timer t;
		private void ScheduleSearchItems(string searchText, int delay)
		{
			TimerCallback tc = SearchItems;
			if (t != null)
				t.Dispose();
			t = new Timer(tc, searchText, delay, Timeout.Infinite);
		}

		async void SearchItems(object state)
		{
			StartDisplayLoading();

			if (_currentList == cache || _currentList == todayItems)
				await LoadItems<CachedItem>(state as String);
			else
				await LoadItems<HistoryItem>(state as String);

			EndDisplayLoading();
		}

		async Task LoadItems<T>(string searchText) where T : class, IWebItem
		{
			var resultItems = await FSDBManager.Instance.GetSortedItemsWhichContain<T>(searchText);
			Dispatcher.BeginInvoke(() =>
			{
				_currentList.ItemsSource = resultItems;
			});
		}

		void StartDisplayLoading()
		{
			Dispatcher.BeginInvoke(() =>
			{
				progressBar.IsIndeterminate = true;
				progressBar.Visibility = Visibility.Visible;
			});
		}

		void EndDisplayLoading()
		{
			Dispatcher.BeginInvoke(() =>
			{
				progressBar.IsIndeterminate = false;
				progressBar.Visibility = Visibility.Collapsed;
			});
		}

		
	}
}