using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using BaconographyWP8Core;
using BaconographyWP8.ViewModel;
using Microsoft.Practices.ServiceLocation;
using BaconographyPortable.Services;
using BaconographyWP8.Common;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using BaconographyWP8.Messages;
using GalaSoft.MvvmLight.Messaging;
using BaconographyPortable.Model.Reddit;
using System.Threading;
using Microsoft.Phone.Reactive;
using BaconographyPortable.Messages;

namespace BaconographyWP8.View
{
	[ViewUri("/View/SortSubredditPageView.xaml")]
	public partial class SortSubredditPageView : PhoneApplicationPage
	{
		public SortSubredditPageView()
		{
			InitializeComponent();
		}

		protected override async void OnNavigatedFrom(NavigationEventArgs e)
		{
			if (e.NavigationMode == NavigationMode.Back)
			{
				Messenger.Default.Send<ReorderSubredditMessage>(new ReorderSubredditMessage());
			}
		}

		private void CloseButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			var button = sender as Button;
			var subreddit = button.DataContext as TypedThing<Subreddit>;
			if (subreddit != null)
				Messenger.Default.Send<CloseSubredditMessage>(new CloseSubredditMessage { Subreddit = subreddit });

			if (subredditList.Items.Count == 0)
			{
				Scheduler.Dispatcher.Schedule(GoBack, TimeSpan.FromSeconds(1.5));
			}
		}

		private void RefreshButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			var button = sender as Button;
			var subreddit = button.DataContext as TypedThing<Subreddit>;
			if (subreddit != null)
				Messenger.Default.Send<RefreshSubredditMessage>(new RefreshSubredditMessage { Subreddit = subreddit });
		}

		private void GoBack()
		{
			ServiceLocator.Current.GetInstance<INavigationService>().GoBack();
		}
	}
}