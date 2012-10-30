
using Callisto.Controls;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using Baconography.Messages;
using Baconography.RedditAPI;
using Baconography.RedditAPI.Things;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Windows.ApplicationModel.Search;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.ApplicationSettings;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Baconography.View
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class RedditView : Baconography.Common.LayoutAwarePage
    {
        SelectSubreddit _selectedSubredditMessage;
        double _scrollOffset;
        public RedditView()
        {
            this.InitializeComponent();
            Messenger.Default.Register<SelectSubreddit>(this, selectedSubreddit =>
            {
                _selectedSubredditMessage = selectedSubreddit;
            });
        }

        ~RedditView()
        {
            Messenger.Default.Unregister<SelectSubreddit>(this);
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

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            if (pageState != null && pageState.ContainsKey("ScrollOffset"))
            {
                _scrollOffset = pageState["ScrollOffset"] as Nullable<double> ?? 0.0;

                linksView.Loaded += linksView_Loaded;
            }

            if (pageState != null && pageState.ContainsKey("SelectedSubredditMessage"))
            {
                var selectedSubredditMessage = pageState["SelectedSubredditMessage"] as SelectSubreddit;
                Messenger.Default.Send<SelectSubreddit>(selectedSubredditMessage);
            }
            else if (navigationParameter != null)
            {
				if (navigationParameter is SelectSubreddit)
				{
					var selectedSubredditMessage = navigationParameter as SelectSubreddit;
					Messenger.Default.Send<SelectSubreddit>(selectedSubredditMessage);
				}
				else if (navigationParameter is string)
				{
					var navString = navigationParameter as string;
					var thing = JsonConvert.DeserializeObject<Thing>(navString);
					if (thing != null)
					{
						var link = thing.Data as Link;
						var subreddit = thing.Data as Subreddit;

						if (link != null)
						{
							var linkMessage = new NavigateToUrlMessage();
							linkMessage.TargetUrl = link.Url;
							linkMessage.Title = link.Title;
							Messenger.Default.Send<NavigateToUrlMessage>(linkMessage);
						}
						else if (subreddit != null)
						{
							var selectSubreddit = new SelectSubreddit();
							var typedSubreddit = new TypedThing<Subreddit>(new Thing { Kind = "t5", Data = subreddit });
							selectSubreddit.Subreddit = new TypedThing<Subreddit>(typedSubreddit);
							Messenger.Default.Send<SelectSubreddit>(selectSubreddit);
						}
					}
				}
            }
            else if(_selectedSubredditMessage != null)
            {
                Messenger.Default.Send<SelectSubreddit>(null);
            }
        }

        void linksView_Loaded(object sender, RoutedEventArgs e)
        {
            var scrollViewer = GetScrollViewer(linksView) as ScrollViewer;
            scrollViewer.ScrollToVerticalOffset(_scrollOffset);
            linksView.Loaded -= linksView_Loaded;
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
            pageState["SelectedSubredditMessage"] = _selectedSubredditMessage;
            var scrollViewer = GetScrollViewer(linksView) as ScrollViewer;
            if (scrollViewer != null)
            {
                _scrollOffset = scrollViewer.VerticalOffset;
                pageState["ScrollOffset"] = _scrollOffset;
            }
        }

        protected override void OnNavigatedTo( NavigationEventArgs e )
        {
            base.OnNavigatedTo( e );
            //this needs to be guarded as the search pane can disappear on us if we're getting dumped out of/suspended
            App.SetSearchKeyboard(true);
        }

        protected override void OnNavigatedFrom( NavigationEventArgs e )
        {
            if (_redditPickerFlyout != null)
                _redditPickerFlyout.IsOpen = false;

            App.SetSearchKeyboard(false);

            base.OnNavigatedFrom( e );
        }

        Flyout _redditPickerFlyout;
        private void ShowRedditPicker(object sender, RoutedEventArgs e)
        {
            _redditPickerFlyout = new Flyout();
            _redditPickerFlyout.Placement = PlacementMode.Bottom;
            _redditPickerFlyout.PlacementTarget = sender as UIElement;
            _redditPickerFlyout.Content = new SubredditPickerControl();
            _redditPickerFlyout.IsOpen = true;
        }
    }
}
