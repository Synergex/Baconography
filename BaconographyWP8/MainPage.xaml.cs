using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using BaconographyWP8.Resources;
using BaconographyPortable.Services;
using Microsoft.Practices.ServiceLocation;
using BaconographyPortable.ViewModel;
using Newtonsoft.Json;
using BaconographyWP8.View;
using GalaSoft.MvvmLight.Messaging;
using BaconographyPortable.Messages;
using BaconographyWP8.Messages;
using BaconographyWP8Core;
using BaconographyWP8.ViewModel;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;

namespace BaconographyWP8
{
	[ViewUri("/MainPage.xaml")]
    public partial class MainPage : PhoneApplicationPage
    {

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();

			Messenger.Default.Register<UserLoggedInMessage>(this, OnUserLoggedIn);
			Messenger.Default.Register<SelectIndexMessage>(this, OnSelectIndexMessage);
        }

		private void AdjustForOrientation(PageOrientation orientation)
		{
			if (orientation == PageOrientation.Landscape
				|| orientation == PageOrientation.LandscapeLeft
				|| orientation == PageOrientation.LandscapeRight)
				SystemTray.IsVisible = false;
			else
				SystemTray.IsVisible = true;

			if (orientation == PageOrientation.LandscapeRight)
				LayoutRoot.Margin = new Thickness(40, 0, 0, 0);
			else if (orientation == PageOrientation.LandscapeLeft)
				LayoutRoot.Margin = new Thickness(0, 0, 35, 0);
			else
				LayoutRoot.Margin = new Thickness(0, 0, 0, 0);
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			this.AdjustForOrientation(this.Orientation);

			if (e.NavigationMode == NavigationMode.Back)
			{
				
			}
            else if (e.NavigationMode == NavigationMode.Refresh)
            {

            }
            else if (e.NavigationMode == NavigationMode.New)
            {
                if (this.NavigationContext.QueryString.ContainsKey("data"))
                {
                    //this appears to be a bug in WP8, the page is getting lazily bound but
                    //we're at a point where it should be completed
                    if (pivot.DataContext != null && pivot.ItemsSource == null)
                    {
                        pivot.ItemsSource = ((MainPageViewModel)pivot.DataContext).PivotItems;
                    }
                    var unescapedData = Uri.UnescapeDataString(this.NavigationContext.QueryString["data"]);
                    var deserializedObject = JsonConvert.DeserializeObject<SelectTemporaryRedditMessage>(unescapedData);
                    if (deserializedObject is SelectTemporaryRedditMessage)
                    {
                        Messenger.Default.Send<SelectTemporaryRedditMessage>(deserializedObject as SelectTemporaryRedditMessage);
                        int indexToPosition;
                        if (pivot.DataContext != null && (((MainPageViewModel)pivot.DataContext).FindSubredditMessageIndex(deserializedObject as SelectTemporaryRedditMessage, out indexToPosition)))
                        {
                            pivot.SelectedIndex = indexToPosition;
                        }
                    }
                }
            }
		}

		protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
		{
			if (sortPopup.IsOpen == true)
			{
				sortPopup.IsOpen = false;
				e.Cancel = true;
			}
			else
			{
				base.OnBackKeyPress(e);
			}
		}

		protected override void OnOrientationChanged(OrientationChangedEventArgs e)
		{
			AdjustForOrientation(e.Orientation);

			base.OnOrientationChanged(e);
		}

		private void OnSelectIndexMessage(SelectIndexMessage message)
		{
			if (message.TypeContext == typeof(MainPageViewModel))
			{
				if (message.Index < pivot.Items.Count && message.Index >= 0)
				{
					pivot.SelectedIndex = message.Index;
				}
				else if (message.Index == -1)
				{
					pivot.SelectedIndex = pivot.Items.Count - 1;
				}
			}
		}

		private string loginItemText = "login";
		private void OnUserLoggedIn(UserLoggedInMessage message)
		{
			bool loggedIn = message.CurrentUser != null && message.CurrentUser.Username != null;

			if (loggedIn)
			{
				loginItemText = "switch user / logout";
			}
			else
			{
				loginItemText = "login";
			}
		}

		private void MenuLogin_Click(object sender, EventArgs e)
		{
			var _navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
			_navigationService.Navigate(typeof(LoginPageView), null);
		}

		private void MenuClose_Click(object sender, EventArgs e)
		{
			var rvm = pivot.SelectedItem as RedditViewModel;
			var trvm = pivot.SelectedItem as TemporaryRedditViewModel;
			if (rvm != null)
			{
				Messenger.Default.Send<CloseSubredditMessage>(new CloseSubredditMessage { Heading = rvm.Heading });
			}
			else if (trvm != null)
			{
				Messenger.Default.Send<CloseSubredditMessage>(new CloseSubredditMessage { Heading = trvm.RedditViewModel.Heading });
			}
		}

		private void MenuSave_Click(object sender, EventArgs e)
		{
			var trvm = pivot.SelectedItem as TemporaryRedditViewModel;
			if (trvm == null)
				return;

			Messenger.Default.Send<CloseSubredditMessage>(new CloseSubredditMessage { Heading = trvm.RedditViewModel.Heading });
			Messenger.Default.Send<SelectSubredditMessage>(new SelectSubredditMessage { Subreddit = trvm.RedditViewModel.SelectedSubreddit });
		}

		private void MenuSettings_Click(object sender, EventArgs e)
		{
			var _navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
			_navigationService.Navigate(typeof(SettingsPageView), null);
		}

		private void MenuSort_Click(object sender, EventArgs e)
		{
			var _navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
			_navigationService.Navigate(typeof(SortSubredditPageView), null);
		}

		Popup sortPopup = new Popup();
		private void ApplicationBar_StateChanged(object sender, ApplicationBarStateChangedEventArgs e)
		{
			if (e.IsMenuVisible)
			{
				var appBarMenu = sender as ApplicationBar;

				if (appBarMenu != null)
				{
					appBarMenu.MenuItems.Clear();

					var login = new ApplicationBarMenuItem();
					login.Text = loginItemText;
					login.Click += MenuLogin_Click;

					var sort = new ApplicationBarMenuItem();
					sort.Text = "sort subreddit";
					sort.Click += (object s, EventArgs args) =>
						{
							sortPopup = new Popup();
							sortPopup.Height = 300;
							sortPopup.Width = 400;
							sortPopup.VerticalOffset = 100;

							var child = new SelectSortTypeView();
							child.button_ok.Click += (object buttonSender, RoutedEventArgs buttonArgs) =>
								{
									sortPopup.IsOpen = false;
								};

							child.button_cancel.Click += (object buttonSender, RoutedEventArgs buttonArgs) =>
								{
									sortPopup.IsOpen = false;
								};
							sortPopup.Child = child;
							sortPopup.IsOpen = true;
						};
					sort.IsEnabled = true;

					var close = new ApplicationBarMenuItem();
					close.Text = "close subreddit";
					close.Click += MenuClose_Click;
					close.IsEnabled = false;

					var rvm = pivot.SelectedItem as RedditViewModel;
					if (rvm != null && pivot.SelectedIndex != 0)
						close.IsEnabled = true;

					ApplicationBarMenuItem save = null;
					var trvm = pivot.SelectedItem as TemporaryRedditViewModel;
					if (trvm != null)
					{
						close.IsEnabled = true;
						save = new ApplicationBarMenuItem();
						save.Text = "save subreddit";
						save.Click += MenuSave_Click;
					}

					var manage = new ApplicationBarMenuItem();
					manage.Text = "manage subreddits";
					manage.Click += MenuSort_Click;
					manage.IsEnabled = true;

					var settings = new ApplicationBarMenuItem();
					settings.Text = "settings";
					settings.Click += MenuSettings_Click;

					if (save != null)
					{
						appBarMenu.MenuItems.Add(save);
						appBarMenu.MenuItems.Add(close);
					}
					appBarMenu.MenuItems.Add(manage);
					appBarMenu.MenuItems.Add(login);
					appBarMenu.MenuItems.Add(sort);
					appBarMenu.MenuItems.Add(settings);

				}
			}
		}

        // Sample code for building a localized ApplicationBar
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Set the page's ApplicationBar to a new instance of ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Create a new button and set the text value to the localized string from AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Create a new menu item with the localized string from AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
    }
}