﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using BaconographyWP8Core;
using BaconographyPortable.Messages;
using GalaSoft.MvvmLight.Messaging;
using BaconographyPortable.ViewModel;
using Windows.ApplicationModel.DataTransfer;
using Newtonsoft.Json;
using BaconographyPortable.Model.Reddit;
using BaconographyWP8.Messages;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using BaconographyPortable.Services;

namespace BaconographyWP8.View
{
	[ViewUri("/View/CommentsView.xaml")]
	public partial class CommentsView : PhoneApplicationPage
	{
		SelectCommentTreeMessage _selectedCommentTree;
		public CommentsView()
		{
			InitializeComponent();
		}

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.New && e.Uri.ToString() == "//MainPage.xaml" && e.IsCancelable)
            {
                e.Cancel = true;
            }
        }

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			if (e.NavigationMode == NavigationMode.Back)
			{
                
			}
            else if (e.NavigationMode == NavigationMode.Reset)
            {
                //do nothing we have everything we want already here
            }
            else
            {
                if (this.State != null && this.State.ContainsKey("SelectedCommentTreeMessage"))
                {
                    _selectedCommentTree = this.State["SelectedCommentTreeMessage"] as SelectCommentTreeMessage;
                    Messenger.Default.Send<SelectCommentTreeMessage>(_selectedCommentTree);
                }
                else if (this.NavigationContext.QueryString["data"] != null)
                {
                    var unescapedData = HttpUtility.UrlDecode(this.NavigationContext.QueryString["data"]);
                    var deserializedObject = JsonConvert.DeserializeObject<SelectCommentTreeMessage>(unescapedData);
                    if (deserializedObject is SelectCommentTreeMessage)
                    {
                        _selectedCommentTree = deserializedObject as SelectCommentTreeMessage;
                        Messenger.Default.Send<SelectCommentTreeMessage>(_selectedCommentTree);
                    }
                }

                RegisterShareSourceContract();
            }
		}


		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			if (e.NavigationMode != NavigationMode.Back)
			{

			}
			else
			{
				this.State["SelectedCommentTreeMessage"] = _selectedCommentTree;
				//UnregisterShareSourceContract();
				//Content = null;
				//((CommentsViewModel)DataContext).Cleanup();
			}
		}

		/// <summary>
		/// Event handler for the DataTransferManager.DataRequested event
		/// </summary>
		private void DataRequestedEventHandler(DataTransferManager sender, DataRequestedEventArgs e)
		{
			var vm = this.DataContext as CommentsViewModel;
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
			if (this.DataContext is CommentsViewModel)
			{
				//DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
				//dataTransferManager.DataRequested += DataRequestedEventHandler;
			}
		}

		/// <summary>
		/// Unregister the current page as a share source
		/// </summary>
		private void UnregisterShareSourceContract()
		{
			if (this.DataContext is CommentsViewModel)
			{
				//DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
				//dataTransferManager.DataRequested -= DataRequestedEventHandler;
			}
		}

		private void ReplyButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			var vm = this.DataContext as CommentsViewModel;
			vm.GotoReply.Execute(this.DataContext);
			var replyData = vm.ReplyData;
			if (SimpleIoc.Default.IsRegistered<ReplyViewModel>())
				SimpleIoc.Default.Unregister<ReplyViewModel>();
			SimpleIoc.Default.Register<ReplyViewModel>(() => replyData, true);
			var _navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
			_navigationService.Navigate(typeof(ReplyViewPage), null);
		}
	}
}
