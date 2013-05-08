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
using BaconographyPortable.Model.Reddit.ListingHelpers;
using BaconographyPortable.ViewModel;
using System.Windows.Data;

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
				if (pinnedSubredditList.Items.Count == 0)
				{
					var frontPage = new TypedThing<Subreddit>(SubredditInfo.GetFrontPageThing());
					Messenger.Default.Send<SelectSubredditMessage>(new SelectSubredditMessage { Subreddit = frontPage });
				}
				else
				{
					Messenger.Default.Send<ReorderSubredditMessage>(new ReorderSubredditMessage());
				}
			}
		}

		private void UnpinButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			var button = sender as Button;
			var subreddit = button.DataContext as TypedThing<Subreddit>;
			if (subreddit != null)
				Messenger.Default.Send<CloseSubredditMessage>(new CloseSubredditMessage { Subreddit = subreddit });

			if (pinnedSubredditList.Items.Count == 0)
			{
				Scheduler.Dispatcher.Schedule(SwitchToNew, TimeSpan.FromSeconds(0.10));
			}
		}

		private void GotoButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			var button = sender as Button;
			var subreddit = button.DataContext as TypedThing<Subreddit>;
			if (subreddit == null && button.DataContext is AboutSubredditViewModel)
				subreddit = (button.DataContext as AboutSubredditViewModel).Thing;
			if (subreddit != null)
			{
				if (pinnedSubredditList.Items.Contains(subreddit))
				{
					Messenger.Default.Send<SelectSubredditMessage>(new SelectSubredditMessage { Subreddit = subreddit });
				}
				else
				{
					Messenger.Default.Send<SelectTemporaryRedditMessage>(new SelectTemporaryRedditMessage { Subreddit = subreddit });
				}

				ServiceLocator.Current.GetInstance<INavigationService>().GoBack();
			}
		}

		private void PinButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			var button = sender as Button;
			var subreddit = button.DataContext as TypedThing<Subreddit>;
			if (subreddit == null && button.DataContext is AboutSubredditViewModel)
				subreddit = (button.DataContext as AboutSubredditViewModel).Thing;
			if (subreddit != null)
			{
				var mpvm = this.DataContext as MainPageViewModel;
				if (mpvm != null)
				{
					Messenger.Default.Send<SelectSubredditMessage>(new SelectSubredditMessage { Subreddit = subreddit, AddOnly = true });
				}
			}
		}

		private void RefreshButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			var button = sender as Button;
			var subreddit = button.DataContext as TypedThing<Subreddit>;
			if (subreddit != null)
				Messenger.Default.Send<RefreshSubredditMessage>(new RefreshSubredditMessage { Subreddit = subreddit });
		}

		private void SwitchToNew()
		{
			pivot.SelectedIndex = 2;
			//ServiceLocator.Current.GetInstance<INavigationService>().GoBack();
		}

		private void manualBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Enter)
			{
				this.Focus();
				var ssvm = this.DataContext as SubredditSelectorViewModel;
				if (ssvm != null)
					ssvm.PinSubreddit.Execute(ssvm);
			}
            else
            {
                BindingExpression bindingExpression = ((TextBox)sender).GetBindingExpression(TextBox.TextProperty);
                if (bindingExpression != null)
                {
                    bindingExpression.UpdateSource();
                }
            }
		}

		private void LoginButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			var _navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
			_navigationService.Navigate(typeof(LoginPageView), null);
		}
	}
}