using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
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
using GalaSoft.MvvmLight;
using BaconographyWP8Core.Common;
using BaconographyWP8.Converters;

namespace BaconographyWP8
{
    [ViewUri("/BaconographyWP8Core;component/MainPage.xaml")]
    public partial class MainPage : PhoneApplicationPage
    {

        // Constructor
		ISettingsService _settingsService;
        IViewModelContextService _viewModelContextService;
        ISmartOfflineService _smartOfflineService;
        INavigationService _navigationService;
        public MainPage()
        {
            InitializeComponent();
            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();

			Messenger.Default.Register<UserLoggedInMessage>(this, OnUserLoggedIn);
			Messenger.Default.Register<SelectIndexMessage>(this, OnSelectIndexMessage);
			_settingsService = ServiceLocator.Current.GetInstance<ISettingsService>();
            _viewModelContextService = ServiceLocator.Current.GetInstance<IViewModelContextService>();
            _smartOfflineService = ServiceLocator.Current.GetInstance<ISmartOfflineService>();
            _navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
        }

		private void AdjustForOrientation(PageOrientation orientation)
		{
			Messenger.Default.Send<OrientationChangedMessage>(new OrientationChangedMessage { Orientation = orientation });
			lastKnownOrientation = orientation;

			if (LayoutRoot != null)
			{
				if (orientation == PageOrientation.LandscapeRight)
					LayoutRoot.Margin = new Thickness(40, 0, 0, 0);
				else if (orientation == PageOrientation.LandscapeLeft)
					LayoutRoot.Margin = new Thickness(0, 0, 35, 0);
				else
					LayoutRoot.Margin = new Thickness(0, 0, 0, 0);
			}
		}

		PageOrientation lastKnownOrientation;


        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            _viewModelContextService.PopViewModelContext();
            base.OnNavigatingFrom(e);
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
                if (this.NavigationContext.QueryString.ContainsKey("data") && !string.IsNullOrWhiteSpace(this.NavigationContext.QueryString["data"]))
                {
                    try
                    {
                        //this appears to be a bug in WP8, the page is getting lazily bound but
                        //we're at a point where it should be completed
                        if (pivot.DataContext != null && pivot.ItemsSource == null)
                        {
                            pivot.ItemsSource = new ReifiedSubredditTemplateCollectionConverter().Convert(((MainPageViewModel)pivot.DataContext).PivotItems, null, null, null) as System.Collections.IEnumerable;
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
                else if (this.NavigationContext.QueryString.ContainsKey("WallpaperSettings"))
                {
                    _navigationService.Navigate<SettingsPageView>("lockscreen");
                    while (NavigationService.BackStack.Count() > 0)
                        NavigationService.RemoveBackEntry();
                    return;
                }
            }

            _viewModelContextService.PushViewModelContext(DataContext as ViewModelBase);
            _smartOfflineService.NavigatedToView(typeof(MainPage), (e.NavigationMode == NavigationMode.New));
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
            if (appMenuItems == null || ApplicationBar.MenuItems.Count == 0)
                BuildMenu();

			bool loggedIn = message.CurrentUser != null && message.CurrentUser.Username != null;

			if (loggedIn)
			{
				loginItemText = "switch user / logout";
			}
			else
			{
				loginItemText = "login";
			}

            appMenuItems[(int)MenuEnum.Login].Text = loginItemText;

            if (loggedIn)
            {
                appBarButtons[(int)ButtonEnum.Mail].IsEnabled = true;
                appMenuItems[(int)MenuEnum.Submit].IsEnabled = true;
            }
            else
            {
                appBarButtons[(int)ButtonEnum.Mail].IsEnabled = false;
                appMenuItems[(int)MenuEnum.Submit].IsEnabled = false;
            }
		}

		private void MenuLogin_Click(object sender, EventArgs e)
		{
			var _navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
			_navigationService.Navigate(typeof(LoginPageView), null);
		}

		private void MenuClose_Click(object sender, EventArgs e)
		{
            var rvm = ((PivotItem)pivot.SelectedItem).DataContext as RedditViewModel;
			if (rvm != null)
			{
				Messenger.Default.Send<CloseSubredditMessage>(new CloseSubredditMessage { Heading = rvm.Heading });
			}
		}

		private void MenuPin_Click(object sender, EventArgs e)
		{
            var trvm = ((PivotItem)pivot.SelectedItem).DataContext as RedditViewModel;
			if (trvm == null || !trvm.IsTemporary)
				return;

			Messenger.Default.Send<CloseSubredditMessage>(new CloseSubredditMessage { Heading = trvm.Heading });
			Messenger.Default.Send<SelectSubredditMessage>(new SelectSubredditMessage { Subreddit = trvm.SelectedSubreddit });
		}

		private void MenuSettings_Click(object sender, EventArgs e)
		{
			var _navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
			_navigationService.Navigate(typeof(SettingsPageView), null);
		}

        private void MenuMail_Click(object sender, EventArgs e)
        {
            var locator = Styles.Resources["Locator"] as ViewModelLocator;
            if (locator != null)
            {
                locator.Messages.RefreshMessages.Execute(locator.Messages);
            }
            var _navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
            _navigationService.Navigate(typeof(MessagingPageView), null);
        }

        private void MenuSubmit_Click(object sender, EventArgs e)
        {
            var locator = Styles.Resources["Locator"] as ViewModelLocator;
            if (locator != null)
            {
                locator.Submit.RefreshUser.Execute(locator.Submit);
            }
            var _navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
            _navigationService.Navigate(typeof(ComposePostPageView), null);
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

			RedditViewModel rvm = ((PivotItem)pivot.SelectedItem).DataContext as RedditViewModel;
			if (rvm == null)
			    return;
			

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
        List<ApplicationBarIconButton> appBarButtons;

		enum MenuEnum
		{
			Login = 0,
            Submit,
			Close,
			Pin
            /*
            Sort,
            Mail,
            Settings,
            Manage,*/
		}

        enum ButtonEnum
        {
            ManageSubreddits = 0,
            Mail,
            Settings,
            Sort
        }

		private void BuildMenu()
		{
            appBarButtons = new List<ApplicationBarIconButton>();
			appMenuItems = new List<ApplicationBarMenuItem>();

            appBarButtons.Add(new ApplicationBarIconButton());
            appBarButtons[(int)ButtonEnum.ManageSubreddits].IconUri = new Uri("\\Assets\\Icons\\manage.png", UriKind.Relative);
            appBarButtons[(int)ButtonEnum.ManageSubreddits].Text = "manage subs";
            appBarButtons[(int)ButtonEnum.ManageSubreddits].IsEnabled = true;
            appBarButtons[(int)ButtonEnum.ManageSubreddits].Click += MenuManage_Click;

            appBarButtons.Add(new ApplicationBarIconButton());
            appBarButtons[(int)ButtonEnum.Mail].IconUri = new Uri("\\Assets\\Icons\\email.png", UriKind.Relative);
            appBarButtons[(int)ButtonEnum.Mail].Text = "mail";
            appBarButtons[(int)ButtonEnum.Mail].IsEnabled = false;
            appBarButtons[(int)ButtonEnum.Mail].Click += MenuMail_Click;

            appBarButtons.Add(new ApplicationBarIconButton());
            appBarButtons[(int)ButtonEnum.Settings].IconUri = new Uri("\\Assets\\Icons\\settings.png", UriKind.Relative);
            appBarButtons[(int)ButtonEnum.Settings].Text = "settings";
            appBarButtons[(int)ButtonEnum.Settings].IsEnabled = true;
            appBarButtons[(int)ButtonEnum.Settings].Click += MenuSettings_Click;

            appBarButtons.Add(new ApplicationBarIconButton());
            appBarButtons[(int)ButtonEnum.Sort].IconUri = new Uri("\\Assets\\Icons\\sort.png", UriKind.Relative);
            appBarButtons[(int)ButtonEnum.Sort].Text = "sort";
            appBarButtons[(int)ButtonEnum.Sort].IsEnabled = true;
            appBarButtons[(int)ButtonEnum.Sort].Click += MenuSort_Click;

            ApplicationBar.Buttons.Clear();
            try
            {
                foreach (var button in appBarButtons)
                    ApplicationBar.Buttons.Add(button as IApplicationBarIconButton);
            }
            catch (Exception e)
            {

            }

			appMenuItems.Add(new ApplicationBarMenuItem());
			appMenuItems[(int)MenuEnum.Login].Text = loginItemText;
			appMenuItems[(int)MenuEnum.Login].IsEnabled = true;
			appMenuItems[(int)MenuEnum.Login].Click += MenuLogin_Click;

            appMenuItems.Add(new ApplicationBarMenuItem());
            appMenuItems[(int)MenuEnum.Submit].Text = "new post";
            appMenuItems[(int)MenuEnum.Submit].IsEnabled = false;
            appMenuItems[(int)MenuEnum.Submit].Click += MenuSubmit_Click;

            /*
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
            */

			appMenuItems.Add(new ApplicationBarMenuItem());
			appMenuItems[(int)MenuEnum.Close].Text = "close subreddit";
			appMenuItems[(int)MenuEnum.Close].IsEnabled = true;
			appMenuItems[(int)MenuEnum.Close].Click += MenuClose_Click;

			appMenuItems.Add(new ApplicationBarMenuItem());
			appMenuItems[(int)MenuEnum.Pin].Text = "pin subreddit";
			appMenuItems[(int)MenuEnum.Pin].IsEnabled = true;
			appMenuItems[(int)MenuEnum.Pin].Click += MenuPin_Click;

            /*
            appMenuItems.Add(new ApplicationBarMenuItem());
            appMenuItems[(int)MenuEnum.Mail].Text = "mail";
            appMenuItems[(int)MenuEnum.Mail].IsEnabled = true;
            appMenuItems[(int)MenuEnum.Mail].Click += MenuMail_Click;
            */            

			ApplicationBar.MenuItems.Clear();
            ApplicationBar.MenuItems.Add(appMenuItems[(int)MenuEnum.Login]);
            ApplicationBar.MenuItems.Add(appMenuItems[(int)MenuEnum.Submit]);
            /*
			ApplicationBar.MenuItems.Add(appMenuItems[(int)MenuEnum.Manage]);
			ApplicationBar.MenuItems.Add(appMenuItems[(int)MenuEnum.Sort]);
			ApplicationBar.MenuItems.Add(appMenuItems[(int)MenuEnum.Settings]);
            */
		}

		private void UpdateMenuItems()
		{
			if (appMenuItems == null || ApplicationBar.MenuItems.Count == 0)
				BuildMenu();

            if (pivot.SelectedItem is PivotItem &&
                ((PivotItem)pivot.SelectedItem).DataContext is RedditViewModel &&
                ((RedditViewModel)((PivotItem)pivot.SelectedItem).DataContext).IsTemporary)
            {
                if (!ApplicationBar.MenuItems.Contains(appMenuItems[(int)MenuEnum.Close]))
                {
                    ApplicationBar.MenuItems.Insert(0, appMenuItems[(int)MenuEnum.Close]);
                    ApplicationBar.MenuItems.Insert(0, appMenuItems[(int)MenuEnum.Pin]);
                }
            }
            else if (ApplicationBar.MenuItems.Contains(appMenuItems[(int)MenuEnum.Close]))
            {
                ApplicationBar.MenuItems.Remove(appMenuItems[(int)MenuEnum.Close]);
                ApplicationBar.MenuItems.Remove(appMenuItems[(int)MenuEnum.Pin]);
            }
		}

		private void OnLoadedPivotItem(object sender, PivotItemEventArgs e)
		{
			UpdateMenuItems();
		}

        int appBarState = 0;
        private void appBar_StateChanged(object sender, ApplicationBarStateChangedEventArgs e)
        {
            if (appBarState == 2)
                appBarState = 0;
            else
                appBarState++;

            switch (appBarState)
            {
                case 0:
                    ApplicationBar.Mode = ApplicationBarMode.Default;
                    break;
                case 1:
                    ApplicationBar.Mode = ApplicationBarMode.Default;
                    break;
                case 2:
                    ApplicationBar.Mode = ApplicationBarMode.Minimized;
                    break;
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