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
    [ViewUri("/BaconographyWP8Core;component/View/SortSubredditPageView.xaml")]
	public partial class SortSubredditPageView : PhoneApplicationPage
	{
		public SortSubredditPageView()
		{
			InitializeComponent();
		}

		const int _offsetKnob = 7;
		private object newListLastItem;
		private object subbedListLastItem;

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.New && e.Uri.ToString() == "/BaconographyWP8Core;component/MainPage.xaml" && e.IsCancelable)
                e.Cancel = true;
            else
                base.OnNavigatingFrom(e);
        }

		void newList_ItemRealized(object sender, ItemRealizationEventArgs e)
		{
			newListLastItem = e.Container.Content;
			var linksView = sender as FixedLongListSelector;
			if (linksView.ItemsSource != null && linksView.ItemsSource.Count >= _offsetKnob)
			{
				if (e.ItemKind == LongListSelectorItemKind.Item)
				{
					if ((e.Container.Content).Equals(linksView.ItemsSource[linksView.ItemsSource.Count - _offsetKnob]))
					{
						var viewModel = DataContext as SubredditSelectorViewModel;
						if (viewModel != null && viewModel.Subreddits.HasMoreItems)
							viewModel.Subreddits.LoadMoreItemsAsync(30);
					}
				}
			}

			var subredditVM = newListLastItem as AboutSubredditViewModel;
			if (subredditVM != null)
			{
				var mainPageVM = this.DataContext as MainPageViewModel;
				var match = mainPageVM.Subreddits.FirstOrDefault<TypedThing<Subreddit>>(thing => thing.Data.DisplayName == subredditVM.Thing.Data.DisplayName);
				if (match != null)
				{
					subredditVM.Pinned = true;
				}
				else
				{
					subredditVM.Pinned = false;
				}
			}
		}

		void subbedList_ItemRealized(object sender, ItemRealizationEventArgs e)
		{
			subbedListLastItem = e.Container.Content;
			var linksView = sender as FixedLongListSelector;
			if (linksView.ItemsSource != null && linksView.ItemsSource.Count >= _offsetKnob)
			{
				if (e.ItemKind == LongListSelectorItemKind.Item)
				{
					if ((e.Container.Content).Equals(linksView.ItemsSource[linksView.ItemsSource.Count - _offsetKnob]))
					{
						var viewModel = DataContext as MainPageViewModel;
                        if (viewModel != null && viewModel.SubscribedSubreddits.HasMoreItems)
                        {
                            viewModel.SubscribedSubreddits.LoadMoreItemsAsync(30);
                        }
					}
				}
			}

			var subredditVM = newListLastItem as AboutSubredditViewModel;
			if (subredditVM != null)
			{
				var mainPageVM = this.DataContext as MainPageViewModel;
				var match = mainPageVM.Subreddits.FirstOrDefault<TypedThing<Subreddit>>(thing => thing.Data.DisplayName == subredditVM.Thing.Data.DisplayName);
				if (match != null)
				{
					subredditVM.Pinned = true;
				}
				else
				{
					subredditVM.Pinned = false;
				}
			}
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
            if (subreddit == null && button.DataContext is SubredditSelectorViewModel)
            {
                var selector = button.DataContext as SubredditSelectorViewModel;
                selector.DoGoSubreddit(pinnedSubredditList.Items.Contains(subreddit));
                ServiceLocator.Current.GetInstance<INavigationService>().GoBack();
            }
			else if (subreddit != null)
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

		private void PinUnpinButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			var button = sender as Button;
			var subredditVM = button.DataContext as AboutSubredditViewModel;
			if (subredditVM != null)
			{
				var mpvm = this.DataContext as MainPageViewModel;
				if (mpvm != null)
				{
					var match = mpvm.Subreddits.FirstOrDefault<TypedThing<Subreddit>>(thing => thing.Data.DisplayName == subredditVM.Thing.Data.DisplayName);
					if (match != null)
					{
						subredditVM.Pinned = false;
						Messenger.Default.Send<CloseSubredditMessage>(new CloseSubredditMessage { Subreddit = subredditVM.Thing });
					}
					else
					{
						subredditVM.Pinned = true;
						Messenger.Default.Send<SelectSubredditMessage>(new SelectSubredditMessage { Subreddit = subredditVM.Thing, AddOnly = true });
					}
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

        //this bit of unpleasantry is needed to prevent the input box from getting defocused when an item gets added to the collection
        bool _disableFocusHack = false;
        bool _needToHackFocus = false;
        TextBox _manualBox = null;
        private void manualBox_LostFocus(object sender, RoutedEventArgs e)
        {
            _manualBox = sender as TextBox;
            if (_disableFocusHack)
                _disableFocusHack = false;
            else
            {
                _needToHackFocus = true;
            }
                //((TextBox)sender).Focus();
        }

        private void manualBox_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            _disableFocusHack = true;
            _needToHackFocus = false;
        }

        private void FixedLongListSelector_GotFocus(object sender, RoutedEventArgs e)
        {
            if(!_disableFocusHack && _needToHackFocus)
            {
                _needToHackFocus = false;
                _manualBox.Focus();
            }
        }
	}
}