using GalaSoft.MvvmLight.Messaging;
using Baconography.Messages;
using Baconography.ViewModel;
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

// The Items Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234233

namespace Baconography.View
{
    /// <summary>
    /// A page that displays a collection of item previews.  In the Split Application this page
    /// is used to display and select one of the available groups.
    /// </summary>
    public sealed partial class SearchResultsView : Baconography.Common.LayoutAwarePage
    {
        private SearchQueryMessage _searchQueryMessage;
        public SearchResultsView()
        {
            this.InitializeComponent();
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
            if (pageState != null && pageState.ContainsKey("SearchQueryMessage"))
            {
                _searchQueryMessage = pageState["SearchQueryMessage"] as SearchQueryMessage;
                Messenger.Default.Send<SearchQueryMessage>(_searchQueryMessage);
            }
            else if (navigationParameter != null)
            {
                if (navigationParameter is SearchQueryMessage)
                {
                    _searchQueryMessage = navigationParameter as SearchQueryMessage;
                    Messenger.Default.Send<SearchQueryMessage>(_searchQueryMessage);
                }
                else if (navigationParameter is string)
                {
                    _searchQueryMessage = new SearchQueryMessage { Query = navigationParameter as string };
                    Messenger.Default.Send<SearchQueryMessage>(_searchQueryMessage);
                }
            }

        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<string, object> pageState)
        {
            pageState["SearchQueryMessage"] = _searchQueryMessage;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            this.LostFocus -= OnLostFocus;
            this.GotFocus -= OnGotFocus;
            SetSearchKeyboard(false);
        }

        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            SetSearchKeyboard(true);
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            SetSearchKeyboard(false);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            this.LostFocus += OnLostFocus;
            this.GotFocus += OnGotFocus;
            SetSearchKeyboard(true);
        }

        private void SetSearchKeyboard(bool value)
        {
            try
            {
                //this needs to be guarded as the search pane can disappear on us if we're getting dumped out of/suspended
                var sp = Windows.ApplicationModel.Search.SearchPane.GetForCurrentView();
                if (sp != null)
                    sp.ShowOnKeyboardInput = value;
            }
            catch
            {
                //do nothing we were most likely shutting down
            }
        }

    }
}
