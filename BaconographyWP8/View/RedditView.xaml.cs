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
using System.Windows.Controls.Primitives;
using BaconographyPortable.Services;
using Microsoft.Practices.ServiceLocation;
using GalaSoft.MvvmLight;

namespace BaconographyWP8.View
{
	[ViewUri("/View/RedditView.xaml")]
	public partial class RedditView : UserControl
	{
		int _offsetKnob = 7;
		object lastItem;

        IViewModelContextService _viewModelContextService;
        ISmartOfflineService _smartOfflineService;
		public RedditView()
		{
			this.InitializeComponent();
            _viewModelContextService = ServiceLocator.Current.GetInstance<IViewModelContextService>();
            _smartOfflineService = ServiceLocator.Current.GetInstance<ISmartOfflineService>();

            _smartOfflineService.NavigatedToView(typeof(RedditView), true);
		}

		void linksView_ItemRealized(object sender, ItemRealizationEventArgs e)
		{
            if (e.ItemKind == LongListSelectorItemKind.Item)
            {
                object o = e.Container.DataContext;
                items[o] = e.Container;
            }

			lastItem = e.Container.Content;
			var linksView = sender as FixedLongListSelector;
			if (linksView.ItemsSource != null && linksView.ItemsSource.Count >= _offsetKnob)
			{
				if (e.ItemKind == LongListSelectorItemKind.Item)
				{
					if ((e.Container.Content).Equals(linksView.ItemsSource[linksView.ItemsSource.Count - _offsetKnob]))
					{
                        var viewModel = DataContext as RedditViewModel;
                        if (viewModel != null && viewModel.Links.HasMoreItems)
                            viewModel.Links.LoadMoreItemsAsync(30);
					}
				}
			}
		}

		void linksView_ItemUnrealized(object sender, ItemRealizationEventArgs e)
		{
            if (e.ItemKind == LongListSelectorItemKind.Item)
            {
                object o = e.Container.DataContext;
                items.Remove(o);
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

        public void UnloadWithScroll()
        {
            if (DataContext is RedditViewModel)
            {
                try
                {
                    ((RedditViewModel)DataContext).TopVisibleLink = GetFirstVisibleItem(this.linksView);
                    linksView.ScrollTo(((RedditViewModel)DataContext).TopVisibleLink);
                    linksView.UpdateLayout();

                    _viewModelContextService.PopViewModelContext(DataContext as ViewModelBase);
                }
                catch
                {
                }
            }
        }

        public void LoadWithScroll()
        {
            try
            {
                if (DataContext is RedditViewModel && ((RedditViewModel)DataContext).TopVisibleLink != null)
                {
                    linksView.UpdateLayout();
                    if (FindViewport(linksView) != null)
                        linksView.ScrollTo(((RedditViewModel)DataContext).TopVisibleLink);

                    _viewModelContextService.PushViewModelContext(DataContext as ViewModelBase);
                }
            }
            catch
            {
            }
        }

        private Dictionary<object, ContentPresenter> items = new Dictionary<object, ContentPresenter>();

        public object GetFirstVisibleItem(LongListSelector lls)
        {
            var viewPort = FindViewport(lls);
            if (items.Count > 0 && viewPort != null)
            {
                var offset = viewPort.Viewport.Top;
                return items.Where(x => Canvas.GetTop(x.Value) + x.Value.ActualHeight > offset)
                    .OrderBy(x => Canvas.GetTop(x.Value)).First().Key;
            }
            else
                return null;
        }

        private static ViewportControl FindViewport(DependencyObject parent)
        {
            var childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (var i = 0; i < childCount; i++)
            {
                var elt = VisualTreeHelper.GetChild(parent, i);
                if (elt is ViewportControl) return (ViewportControl)elt;
                var result = FindViewport(elt);
                if (result != null) return result;
            }
            return null;
        }

	}
}
