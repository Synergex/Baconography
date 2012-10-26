using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.ServiceLocation;
using Baconography.Common;
using Baconography.Messages;
using Baconography.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Baconography.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LinkedWebView : Baconography.Common.LayoutAwarePage
    {

        NavigateToUrlMessage _navigateToUrlMessage;
        public LinkedWebView()
        {
            this.InitializeComponent();
            ServiceLocator.Current.GetInstance<LinkedWebViewModel>().WebView = new WebViewWrapper(this.theWebView);
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
            if (pageState != null && pageState.ContainsKey("NavigateToUrlMessage"))
            {
                _navigateToUrlMessage = pageState["NavigateToUrlMessage"] as NavigateToUrlMessage;
                Messenger.Default.Send<NavigateToUrlMessage>(_navigateToUrlMessage);
            }
            else if (navigationParameter != null && navigationParameter is NavigateToUrlMessage)
            {
                _navigateToUrlMessage = navigationParameter as NavigateToUrlMessage;
                Messenger.Default.Send<NavigateToUrlMessage>(_navigateToUrlMessage);
            }
			RegisterShareSourceContract();
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
            pageState["NavigateToUrlMessage"] = _navigateToUrlMessage;
			UnregisterShareSourceContract();
        }

        WebViewBrush webViewBrush = null;
        private void OnAppBarOpened(object sender, object e)
        {
            if (webViewBrush == null)
            {
                webViewBrush = new WebViewBrush();
                webViewBrush.SetSource(theWebView);
            }

            webViewBrush.Redraw();
            contentViewRect.Fill = webViewBrush;
            theWebView.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void OnAppBarClosed(object sender, object e)
        {
            theWebView.Visibility = Windows.UI.Xaml.Visibility.Visible;
            contentViewRect.Fill = new SolidColorBrush(Windows.UI.Colors.Transparent);
        }

		/// <summary>
		/// Event handler for the DataTransferManager.DataRequested event
		/// </summary>
		private void DataRequestedEventHandler(DataTransferManager sender, DataRequestedEventArgs e)
		{
			LinkedWebViewModel vm = this.DataContext as LinkedWebViewModel;
			if (vm.Source != null)
			{
				DataPackage requestData = e.Request.Data;
				requestData.Properties.Title = vm.LinkedTitle;
				//requestData.Properties.Description = string.Empty;   // optional
				requestData.SetUri(new Uri(vm.Source));
			}
		}

        /// <summary>
        /// Register the current page as a share source
        /// </summary>
        private void RegisterShareSourceContract()
        {
			if (this.DataContext is LinkedWebViewModel)
            {
                DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
                dataTransferManager.DataRequested += DataRequestedEventHandler;
            }
        }

		/// <summary>
		/// Unregister the current page as a share source
		/// </summary>
		private void UnregisterShareSourceContract()
		{
			if (this.DataContext is LinkedWebViewModel)
			{
				DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
				dataTransferManager.DataRequested -= DataRequestedEventHandler;
			}
		}
    }
}
