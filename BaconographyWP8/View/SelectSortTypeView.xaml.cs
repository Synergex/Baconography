using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace BaconographyWP8.View
{
	public partial class SelectSortTypeView : UserControl
	{
		public SelectSortTypeView()
		{
			InitializeComponent();
		}

		public static readonly DependencyProperty SortOrderProperty =
			DependencyProperty.Register(
				"SortOrder",
				typeof(string),
				typeof(SelectSortTypeView),
				new PropertyMetadata("", OnSortOrderPropertyChanged)
			);

		public string SortOrder
		{
			get
			{
				return (string)GetValue(SortOrderProperty);
			}
			set
			{
				SetValue(SortOrderProperty, value);
				if (onCheckOrigin)
					return;
				switch (value)
				{
					case "/new/":
						newRad.IsChecked = true;
						break;
					case "/top/":
						topRad.IsChecked = true;
						break;
					case "/rising/":
						risingRad.IsChecked = true;
						break;
					case "/controversial/":
						controversialRad.IsChecked = true;
						break;
					default:
						hotRad.IsChecked = true;
						break;
				}
			}
		}

		private static void OnSortOrderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var view = (SelectSortTypeView)d;
			view.SortOrder = (string)e.NewValue;
		}

		bool onCheckOrigin = false;
		private void OnChecked(object sender, RoutedEventArgs e)
		{
			var button = sender as RadioButton;
			var content = button.Content as string;

			if (content != null)
			{
				onCheckOrigin = true;
				SortOrder = content;
				onCheckOrigin = false;
			}
		}

	}
}
