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
		int _offsetKnob = 7;
		object lastItem;

		public RedditView()
		{
			this.InitializeComponent();
		}

		void linksView_ItemRealized(object sender, ItemRealizationEventArgs e)
		{
			lastItem = e.Container.Content;
			var linksView = sender as FixedLongListSelector;
			if (linksView.ItemsSource != null && linksView.ItemsSource.Count >= _offsetKnob)
			{
				if (e.ItemKind == LongListSelectorItemKind.Item)
				{
					if ((e.Container.Content as LinkViewModel).Equals(linksView.ItemsSource[linksView.ItemsSource.Count - _offsetKnob]))
					{
                        var viewModel = DataContext as RedditViewModel;
                        if (viewModel != null)
                            viewModel.Links.LoadMoreItemsAsync(30);
					}
				}
			}
		}

		void linksView_ItemUnrealized(object sender, ItemRealizationEventArgs e)
		{
			var height = e.Container.ActualHeight;
		}

		void linksView_Loaded(object sender, RoutedEventArgs e)
		{
			try
			{
				//var linksView = sender as FixedLongListSelector;
				//linksView.Loaded -= linksView_Loaded;
				//linksView.ScrollTo(lastItem);
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
		}

		private void FixedLongListSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var list = sender as FixedLongListSelector;
		}
	}
}
