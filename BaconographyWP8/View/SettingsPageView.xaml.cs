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
using GalaSoft.MvvmLight.Messaging;
using BaconographyPortable.Messages;
using BaconographyPortable.ViewModel;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Media;
using BaconographyWP8BackgroundControls.View;
using System.Text;

namespace BaconographyWP8.View
{
	[ViewUri("/View/SettingsPageView.xaml")]
	public partial class SettingsPageView : PhoneApplicationPage
	{
		public SettingsPageView()
		{
			InitializeComponent();
		}

        protected void OpenHelp(string topic, string content)
        {
            double height = LayoutRoot.ActualHeight - 24;
            double width = LayoutRoot.ActualWidth - 24;

            helpPopup.Height = height;
            helpPopup.Width = width;

            var child = helpPopup.Child as HelpView;
            if (child == null)
                child = new HelpView();
            child.Height = height;
            child.Width = width;
            child.Topic = topic;
            child.Content = content;

            helpPopup.Child = child;
            helpPopup.IsOpen = true;
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (helpPopup.IsOpen == true)
            {
                helpPopup.IsOpen = false;
                e.Cancel = true;
            }
            else
            {
                base.OnBackKeyPress(e);
            }
        }

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			Messenger.Default.Send<SettingsChangedMessage>(new SettingsChangedMessage());
			base.OnNavigatedFrom(e);
		}

		private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
		{
			var _navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
			var hyperlinkButton = e.OriginalSource as ContextDataButton;
			if (hyperlinkButton != null)
			{
				_navigationService.NavigateToExternalUri(new Uri((string)hyperlinkButton.ContextData));
			}
		}

		private void OrientationLock_Checked(object sender, RoutedEventArgs e)
		{
			var preferences = this.DataContext as ContentPreferencesViewModel;
			if (preferences != null)
			{
				preferences.Orientation = this.Orientation.ToString();
			}
		}

		private void OrientationLock_Unchecked(object sender, RoutedEventArgs e)
		{
			var preferences = this.DataContext as ContentPreferencesViewModel;
			if (preferences != null)
			{
				preferences.Orientation = this.Orientation.ToString();
			}
		}

        private async void ShowSystemLockScreenSettings(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-lock:"));
        }

        private async void ShowLockScreenPreview(object sender, RoutedEventArgs e)
        {
            var userService = ServiceLocator.Current.GetInstance<IUserService>();
            var settingsService = ServiceLocator.Current.GetInstance<ISettingsService>();

            await Utility.DoActiveLockScreen(settingsService, ServiceLocator.Current.GetInstance<IRedditService>(), userService,
                ServiceLocator.Current.GetInstance<IImagesService>(), ServiceLocator.Current.GetInstance<INotificationService>(), true);

            var _navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
            _navigationService.Navigate<LockScreen>(null);
        }

        

        private async void SetLockScreen(object sender, RoutedEventArgs e)
        {
            var userService = ServiceLocator.Current.GetInstance<IUserService>();
            var settingsService = ServiceLocator.Current.GetInstance<ISettingsService>();

            await Utility.DoActiveLockScreen(settingsService, ServiceLocator.Current.GetInstance<IRedditService>(), userService,
                ServiceLocator.Current.GetInstance<IImagesService>(), ServiceLocator.Current.GetInstance<INotificationService>(), false);
            
        }

        private void HelpOfflineButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            OpenHelp(
                "OFFLINE CONTENT",
                "The predictive offline cache aggregates usage statistics about the subreddits, links and comments that you click on. This data is stored only on your device and can be erased at any time. We use this data to intelligently guess which links you are likely to click in order to cache the relevant data locally on your device. When you then click on a cached link, the data is loaded very quickly from your device instead of from the web."
                + "\r\n\r\n" +
                "Overnight offline cache is an extension of the predictive cache. When your device is plugged in and connected to Wi-Fi, we can safely download more data at a faster rate. If you enable this option, we will run a background process during optimal conditions to download more reddit goodness."
                );
        }
	}
}