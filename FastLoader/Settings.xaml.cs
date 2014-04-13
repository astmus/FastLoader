﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;

namespace FastLoader
{
	public partial class SettingsPage : PhoneApplicationPage
	{
		public static event Action ClearCachePressed;

		public SettingsPage()
		{
			this.DataContext = AppSettings.Instance;
			InitializeComponent();
			appName.Text += " (v 1.1.5.18)";
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			MarketplaceReviewTask marketplaceReviewTask = new MarketplaceReviewTask();
			marketplaceReviewTask.Show();
		}

		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			if (ClearCachePressed != null)
				ClearCachePressed();
		}
	}
}