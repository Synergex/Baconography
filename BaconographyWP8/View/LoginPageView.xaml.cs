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
				{
					pivot.SelectedIndex = 1;
				}

				vm.LoadCredentials();
			}
			base.OnNavigatedTo(e);
		}
	}
}