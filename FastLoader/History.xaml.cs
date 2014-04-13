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

namespace FastLoader
{
	public partial class History : PhoneApplicationPage
	{
		HistoryItem _item;
		public History()
		{
			InitializeComponent();
		}

		private void SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			
		}

		private void history_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			
			int i = 0;
		}

		private void Grid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			_item = (sender as FrameworkElement).DataContext as HistoryItem;
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