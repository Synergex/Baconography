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
using Microsoft.Phone.Reactive;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BaconographyWP8
{
	[ViewUri("/MainPage.xaml")]
    public partial class MainPage : PhoneApplicationPage
    {

        // Constructor
		ISettingsService _settingsService;
        public MainPage()
        {
            InitializeComponent();
            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();

			Messenger.Default.Register<UserLoggedInMessage>(this, OnUserLoggedIn);
			Messenger.Default.Register<SelectIndexMessage>(this, OnSelectIndexMessage);
			_settingsService = ServiceLocator.Current.GetInstance<ISettingsService>();
        }

		private void AdjustForOrientation(PageOrientation orientation)
		{
			Messenger.Default.Send<OrientationChangedMessage>(new OrientationChangedMessage { Orientation = orientation });
			lastKnownOrientation = orientation;

			if (orientation == PageOrientation.LandscapeRight)
				LayoutRoot.Margin = new Thickness(40, 0, 0, 0);
			else if (orientation == PageOrientation.LandscapeLeft)
				LayoutRoot.Margin = new Thickness(0, 0, 35, 0);
			else
				LayoutRoot.Margin = new Thickness(0, 0, 0, 0);
		}

		PageOrientation lastKnownOrientation;


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
                if (this.NavigationContext.QueryString.ContainsKey("data") && !string.IsNullOrWhiteSpace(this.NavigationContext.QueryString["data"]))
                {
                    try
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
                    catch (UriFormatException)
                    {
                        ServiceLocator.Current.GetInstance<IBaconProvider>().GetService<INotificationService>().CreateNotification("Invalid main page uri state, please PM /u/hippiehunter with details");
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

            if(appMenuItems != null && appMenuItems.Count > (int)MenuEnum.Login)
                appMenuItems[(int)MenuEnum.Login].Text = loginItemText;
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

		private void MenuPin_Click(object sender, EventArgs e)
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

		private void MenuManage_Click(object sender, EventArgs e)
		{
			var _navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
			_navigationService.Navigate(typeof(SortSubredditPageView), null);
		}

		private void MenuSort_Click(object sender, EventArgs e)
		{
			double height = 480;
			double width = 325;

			if (LayoutRoot.ActualHeight <= 480)
				height = LayoutRoot.ActualHeight;
			
			sortPopup.Height = height;
			sortPopup.Width = width;

			RedditViewModel rvm = pivot.SelectedItem as RedditViewModel;
			if (rvm == null)
			{
				var trvm = pivot.SelectedItem as TemporaryRedditViewModel;
				if (trvm != null)
					rvm = trvm.RedditViewModel;
				else
					return;
			}

			var child = sortPopup.Child as SelectSortTypeView;
			if (child == null)
				child = new SelectSortTypeView();
			child.SortOrder = rvm.SortOrder;
			child.Height = height;
			child.Width = width;
			child.button_ok.Click += (object buttonSender, RoutedEventArgs buttonArgs) =>
			{
				sortPopup.IsOpen = false;
				rvm.SortOrder = child.SortOrder;
			};

			child.button_cancel.Click += (object buttonSender, RoutedEventArgs buttonArgs) =>
			{
				sortPopup.IsOpen = false;
			};

			sortPopup.Child = child;
			sortPopup.IsOpen = true;
		}

		List<ApplicationBarMenuItem> appMenuItems;

		enum MenuEnum
		{
			Login = 0,
			Sort,
			Settings,
			Manage,
			Close,
			Pin
		}

		private void BuildMenu()
		{
			appMenuItems = new List<ApplicationBarMenuItem>();

			appMenuItems.Add(new ApplicationBarMenuItem());
			appMenuItems[(int)MenuEnum.Login].Text = loginItemText;
			appMenuItems[(int)MenuEnum.Login].IsEnabled = true;
			appMenuItems[(int)MenuEnum.Login].Click += MenuLogin_Click;

			appMenuItems.Add(new ApplicationBarMenuItem());
			appMenuItems[(int)MenuEnum.Sort].Text = "sort";
			appMenuItems[(int)MenuEnum.Sort].IsEnabled = true;
			appMenuItems[(int)MenuEnum.Sort].Click += MenuSort_Click;

			appMenuItems.Add(new ApplicationBarMenuItem());
			appMenuItems[(int)MenuEnum.Settings].Text = "settings";
			appMenuItems[(int)MenuEnum.Settings].IsEnabled = true;
			appMenuItems[(int)MenuEnum.Settings].Click += MenuSettings_Click;

			appMenuItems.Add(new ApplicationBarMenuItem());
			appMenuItems[(int)MenuEnum.Manage].Text = "manage subreddits";
			appMenuItems[(int)MenuEnum.Manage].IsEnabled = true;
			appMenuItems[(int)MenuEnum.Manage].Click += MenuManage_Click;

			appMenuItems.Add(new ApplicationBarMenuItem());
			appMenuItems[(int)MenuEnum.Close].Text = "close subreddit";
			appMenuItems[(int)MenuEnum.Close].IsEnabled = true;
			appMenuItems[(int)MenuEnum.Close].Click += MenuClose_Click;

			appMenuItems.Add(new ApplicationBarMenuItem());
			appMenuItems[(int)MenuEnum.Pin].Text = "pin subreddit";
			appMenuItems[(int)MenuEnum.Pin].IsEnabled = true;
			appMenuItems[(int)MenuEnum.Pin].Click += MenuPin_Click;

			ApplicationBar.MenuItems.Clear();
			ApplicationBar.MenuItems.Add(appMenuItems[(int)MenuEnum.Manage]);
			ApplicationBar.MenuItems.Add(appMenuItems[(int)MenuEnum.Sort]);
			ApplicationBar.MenuItems.Add(appMenuItems[(int)MenuEnum.Login]);
			ApplicationBar.MenuItems.Add(appMenuItems[(int)MenuEnum.Settings]);
		}

		private void UpdateMenuItems()
		{
			if (appMenuItems == null || ApplicationBar.MenuItems.Count == 0)
				BuildMenu();

			if (pivot.SelectedItem is TemporaryRedditViewModel)
			{
				if (ApplicationBar.MenuItems.Count == 4)
				{
					ApplicationBar.MenuItems.Insert(0, appMenuItems[(int)MenuEnum.Close]);
					ApplicationBar.MenuItems.Insert(0, appMenuItems[(int)MenuEnum.Pin]);
				}
			}
			else if (ApplicationBar.MenuItems.Count > 4)
			{
				ApplicationBar.MenuItems.RemoveAt(0);
				ApplicationBar.MenuItems.RemoveAt(0);
			}
		}

		private void OnLoadedPivotItem(object sender, PivotItemEventArgs e)
		{
			UpdateMenuItems();
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