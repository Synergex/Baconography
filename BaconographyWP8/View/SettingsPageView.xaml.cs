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

namespace BaconographyWP8.View
{
	[ViewUri("/View/SettingsPageView.xaml")]
	public partial class SettingsPageView : PhoneApplicationPage
	{
		public SettingsPageView()
		{
			InitializeComponent();
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
            await BackgroundTask.MakeLockScreenControl(ServiceLocator.Current.GetInstance<ISettingsService>(), ServiceLocator.Current.GetInstance<IRedditService>(), ServiceLocator.Current.GetInstance<IUserService>(),
                ServiceLocator.Current.GetInstance<IImagesService>(), ServiceLocator.Current.GetInstance<IBaconProvider>());

            var _navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
            _navigationService.Navigate<LockScreen>(null);
        }

        private async void SetLockScreen(object sender, RoutedEventArgs e)
        {
            var settingsService = ServiceLocator.Current.GetInstance<ISettingsService>();
            await BackgroundTask.MakeLockScreenControl(settingsService, ServiceLocator.Current.GetInstance<IRedditService>(), ServiceLocator.Current.GetInstance<IUserService>(),
                ServiceLocator.Current.GetInstance<IImagesService>(), ServiceLocator.Current.GetInstance<IBaconProvider>());

            var vml = new ViewModelLocator();
            var lockScreenView = new LockScreen();
            lockScreenView.Width = settingsService.ScreenWidth;
            lockScreenView.Height = settingsService.ScreenHeight;
            lockScreenView.UpdateLayout();
            WriteableBitmap bitmap = new WriteableBitmap(settingsService.ScreenWidth, settingsService.ScreenHeight);
            bitmap.Render(lockScreenView, new ScaleTransform() { ScaleX = 1, ScaleY = 1 });
            bitmap.Invalidate();
            string targetFilePath = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreen.jpg";
            if (File.Exists(targetFilePath))
            {
                targetFilePath = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreenAlt.jpg";
            }
            var lockscreenJpg = File.Create(targetFilePath);
            bitmap.SaveJpeg(lockscreenJpg, settingsService.ScreenWidth, settingsService.ScreenHeight, 0, 100);
            lockscreenJpg.Flush(true);
            lockscreenJpg.Close();
            BackgroundTask.LockHelper(Path.GetFileName(targetFilePath), false);

            if (targetFilePath.EndsWith("lockscreenAlt.jpg") && File.Exists(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreen.jpg"))
            {
                File.Delete(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreen.jpg");
            }
            else if (targetFilePath.EndsWith("lockscreen.jpg") && File.Exists(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreenAlt.jpg"))
            {
                File.Delete(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreenAlt.jpg");
            }

            
            vml.LockScreen.ImageSource = new BitmapImage(new Uri(targetFilePath));
            var _navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
            _navigationService.Navigate<LockScreen>(null);
        }
	}
}