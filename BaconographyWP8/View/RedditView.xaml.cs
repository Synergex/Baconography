using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using BaconographyPortable.ViewModel;
using System.Windows.Media;
using BaconographyWP8Core;
using BaconographyWP8.Converters;
using BaconographyWP8.Common;

namespace BaconographyWP8.View
{
	[ViewUri("/View/RedditView.xaml")]
	public partial class RedditView : UserControl
	{
		RedditViewModel _viewModel;
		int _offsetKnob = 7;
		double _scrollViewOffset = 0;

		public RedditView()
		{
			this.InitializeComponent();
			_viewModel = DataContext as RedditViewModel;
			
		}

		void linksView_ItemRealized(object sender, ItemRealizationEventArgs e)
		{
			var linksView = sender as FixedLongListSelector;
			if (linksView.ItemsSource != null && linksView.ItemsSource.Count >= _offsetKnob)
			{
				if (e.ItemKind == LongListSelectorItemKind.Item)
				{
					if ((e.Container.Content as LinkViewModel).Equals(linksView.ItemsSource[linksView.ItemsSource.Count - _offsetKnob]))
					{
						_viewModel.Links.LoadMoreItemsAsync(30);
					}
				}
			}
		}

		void linksView_ItemUnrealized(object sender, ItemRealizationEventArgs e)
		{
			var height = e.Container.ActualHeight;
		}

		public static DependencyObject GetScrollViewer(DependencyObject o)
		{
			// Return the DependencyObject if it is a ScrollViewer
			if (o is ScrollViewer)
			{ return o; }

			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
			{
				var child = VisualTreeHelper.GetChild(o, i);

				var result = GetScrollViewer(child);
				if (result == null)
				{
					continue;
				}
				else
				{
					return result;
				}
			}
			return null;
		}

		void linksView_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				var linksView = sender as FixedLongListSelector;
				var scrollViewer = GetScrollViewer(linksView) as ScrollViewer;
				if (scrollViewer != null)
					scrollViewer.ScrollToVerticalOffset(_scrollViewOffset);
				linksView.Loaded -= linksView_Loaded;
			}
			catch
			{

			}
		}

		public void button_Click(object sender, RoutedEventArgs e)
		{
			var linksView = sender as FixedLongListSelector;
			var collection = linksView.ItemsSource as PortableAsyncCollectionConverter;
			if (collection != null)
				collection = null;
		}

		private void OnRefresh(object sender, RoutedEventArgs e)
		{
			var linksView = sender as FixedLongListSelector;
			var scrollViewer = GetScrollViewer(linksView) as ScrollViewer;
			_scrollViewOffset = 0;
			scrollViewer.ScrollToVerticalOffset(0);
		}
	}
}
