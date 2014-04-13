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

namespace FastLoader
{
	public partial class History : PhoneApplicationPage
	{
		IWebItem _item;
		ApplicationBarIconButton select;
		ApplicationBarIconButton delete;
		public History()
		{
			InitializeComponent();
			AppBar();
		}

		private void SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			switch (MainPivot.SelectedIndex)
			{
				case 0:
					if (cache.ItemsSource == null)
						cache.ItemsSource = FSDBManager.Instance.GetSortedItems<CachedItem>();
					break;
				case 1:
					if (history.ItemsSource == null)
						history.ItemsSource = FSDBManager.Instance.GetSortedItems<HistoryItem>();
					break;
			}				
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
		}

		private void SetupEmailApplicationBar()
		{
			ClearApplicationBar();

			if (cache.IsSelectionEnabled)
			{
				ApplicationBar.Buttons.Add(delete);
				UpdateApplicationBar();
			}
			else
			{
				ApplicationBar.Buttons.Add(select);
			}
			ApplicationBar.IsVisible = true;
		}

		private void UpdateApplicationBar()
		{
			if (cache.IsSelectionEnabled)
			{
				bool hasSelection = ((cache.SelectedItems != null) && (cache.SelectedItems.Count > 0));
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
			IList source = cache.ItemsSource as IList;
			while (cache.SelectedItems.Count > 0)
			{
				source.Remove((CachedItem)cache.SelectedItems[0]);
			}
		}

		void OnSelectClick(object sender, EventArgs e)
		{
			cache.IsSelectionEnabled = true;
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

		private void history_IsSelectionEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			SetupEmailApplicationBar();
		}

		private void cache_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (cache.IsSelectionEnabled)
			{
				bool hasSelection = ((cache.SelectedItems != null) && (cache.SelectedItems.Count > 0));
				delete.IsEnabled = hasSelection;				
			}
		}
	}
}