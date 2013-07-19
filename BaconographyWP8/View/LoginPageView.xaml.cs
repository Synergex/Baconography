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

namespace BaconographyWP8.View
{
	[ViewUri("/View/LoginPageView.xaml")]
	public partial class LoginPageView : PhoneApplicationPage
	{
		public LoginPageView()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var vm = ServiceLocator.Current.GetInstance<LoginPageViewModel>() as LoginPageViewModel;
			if (vm != null)
			{
				if (!String.IsNullOrEmpty(vm.CurrentUserName))
					pivot.SelectedIndex = 1;

				vm.LoadCredentials();

				if (vm.Credentials.Count > 0)
					pivot.SelectedIndex = 1;
			}
			base.OnNavigatedTo(e);
		}

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.New && e.Uri.ToString() == "//MainPage.xaml" && e.IsCancelable)
                e.Cancel = true;
            else
                base.OnNavigatingFrom(e);
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

		private void Password_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Enter)
			{
				this.Focus();
				var loginVM = this.DataContext as LoginPageViewModel;
                if (loginVM != null)
                {
                    //this keydown seems to happen before the vm gets updates
                    loginVM.Password = this.passwordBox.Password;
                    loginVM.DoLogin.Execute(null);
                }
			}
		}

		private void Username_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Enter ||
				e.Key == System.Windows.Input.Key.Tab)
			{
				passwordBox.Focus();
			}
		}
	}
}