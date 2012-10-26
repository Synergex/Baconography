using GalaSoft.MvvmLight.Messaging;
using Baconography.Messages;
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

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace Baconography.View
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class CommentsView : Baconography.Common.LayoutAwarePage
    {
        SelectCommentTree _selectedCommentTree;
        public CommentsView()
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
            if (pageState != null && pageState.ContainsKey("SelectedCommentTreeMessage"))
            {
                _selectedCommentTree = pageState["SelectedCommentTreeMessage"] as SelectCommentTree;
                Messenger.Default.Send<SelectCommentTree>(_selectedCommentTree);
            }
            else if (navigationParameter != null && navigationParameter is SelectCommentTree)
            {
                _selectedCommentTree = navigationParameter as SelectCommentTree;
                Messenger.Default.Send<SelectCommentTree>(_selectedCommentTree);
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
            pageState["SelectedCommentTreeMessage"] = _selectedCommentTree;
            UnregisterShareSourceContract();
        }

        /// <summary>
        /// Event handler for the DataTransferManager.DataRequested event
        /// </summary>
        private void DataRequestedEventHandler(DataTransferManager sender, DataRequestedEventArgs e)
        {
            var vm = this.DataContext as Baconography.ViewModel.CommentsViewModel;
            if (vm.Url != null)
            {
                DataPackage requestData = e.Request.Data;
                requestData.Properties.Title = vm.Title;
                requestData.SetUri(new Uri(vm.Url));
            }
        }

        /// <summary>
        /// Register the current page as a share source
        /// </summary>
        private void RegisterShareSourceContract()
        {
            if (this.DataContext is Baconography.ViewModel.CommentsViewModel)
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
            if (this.DataContext is Baconography.ViewModel.CommentsViewModel)
            {
                DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
                dataTransferManager.DataRequested -= DataRequestedEventHandler;
            }
        }
    }
}
