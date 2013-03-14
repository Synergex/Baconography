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

namespace BaconographyWP8
{
    public partial class MainPage : PhoneApplicationPage
    {
		bool isNewInstance;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
			isNewInstance = true;
            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();

			Messenger.Default.Register<UserLoggedInMessage>(this, OnUserLoggedIn);
        }

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{

			if (isNewInstance)
				isNewInstance = false;
		}


		protected override async void OnNavigatedFrom(NavigationEventArgs e)
		{
			var mpvm = ServiceLocator.Current.GetInstance<MainPageViewModel>() as MainPageViewModel;
			await mpvm.SaveSubreddits();
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
			var item = pivot.SelectedItem as RedditViewModel;
			if (item == null)
				return;

			Messenger.Default.Send<CloseSubredditMessage>(new CloseSubredditMessage { Heading = item.Heading });
		}

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

					var close = new ApplicationBarMenuItem();
					close.Text = "close subreddit";
					close.Click += MenuClose_Click;
					close.IsEnabled = false;

					var item = pivot.SelectedItem as RedditViewModel;
					if (item != null && pivot.SelectedIndex != 0)
						close.IsEnabled = true;

					appBarMenu.MenuItems.Add(login);
					appBarMenu.MenuItems.Add(close);
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