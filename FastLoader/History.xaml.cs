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

namespace FastLoader
{
	public partial class History : PhoneApplicationPage
	{
		IWebItem _item;
		public History()
		{
			InitializeComponent();
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

		private void history_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			
			int i = 0;
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
	}
}