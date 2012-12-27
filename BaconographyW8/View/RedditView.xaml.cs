﻿using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using BaconographyW8.Common;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace BaconographyW8.View
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class RedditView : BaconographyW8.Common.LayoutAwarePage
    {
        SelectSubredditMessage _selectedSubredditMessage;
        double _scrollOffset;

        public RedditView()
        {
            this.InitializeComponent();
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
                _selectedSubredditMessage = pageState["SelectedSubredditMessage"] as SelectSubredditMessage;
                Messenger.Default.Send<SelectSubredditMessage>(_selectedSubredditMessage);
            }
            else if (navigationParameter != null)
            {
                if (navigationParameter is SelectSubredditMessage)
                {
                    _selectedSubredditMessage = navigationParameter as SelectSubredditMessage;
                    Messenger.Default.Send<SelectSubredditMessage>(_selectedSubredditMessage);
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
                            var selectSubreddit = new SelectSubredditMessage();
                            var typedSubreddit = new TypedThing<Subreddit>(new Thing { Kind = "t5", Data = subreddit });
                            selectSubreddit.Subreddit = new TypedThing<Subreddit>(typedSubreddit);
                            Messenger.Default.Send<SelectSubredditMessage>(selectSubreddit);
                        }
                    }
                }
            }
            else
            {
                Messenger.Default.Send<SelectSubredditMessage>(null);
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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            //this needs to be guarded as the search pane can disappear on us if we're getting dumped out of/suspended
            App.SetSearchKeyboard(true);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (_redditPickerFlyout != null)
                _redditPickerFlyout.IsOpen = false;

            App.SetSearchKeyboard(false);

            base.OnNavigatedFrom(e);
        }

        Flyout _redditPickerFlyout;
        private void ShowRedditPicker(object sender, RoutedEventArgs e)
        {
            App.SetSearchKeyboard(false);
            _redditPickerFlyout = new Flyout();
            _redditPickerFlyout.Width = 430;
            _redditPickerFlyout.Closed += (obj1, obj2) => App.SetSearchKeyboard(true);
            _redditPickerFlyout.Placement = PlacementMode.Bottom;
            _redditPickerFlyout.PlacementTarget = sender as UIElement;
            _redditPickerFlyout.Content = new SubredditPickerView();
            _redditPickerFlyout.IsOpen = true;
        }

        private void OnRefresh(object sender, RoutedEventArgs e)
        {
            var scrollViewer = GetScrollViewer(linksView) as ScrollViewer;
            _scrollOffset = 0;
            scrollViewer.ScrollToVerticalOffset(0);
        }
    }
}
