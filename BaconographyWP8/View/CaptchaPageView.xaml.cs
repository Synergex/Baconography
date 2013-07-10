
using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
using BaconographyWP8Core;
using Microsoft.Phone.Controls;
using Microsoft.Practices.ServiceLocation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Windows.Foundation;
using Windows.Foundation.Collections;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BaconographyWP8.View
{
	[ViewUri("/View/CaptchaPageView.xaml")]
    public sealed partial class CaptchaPageView : PhoneApplicationPage
    {
		INavigationService _navigationService;

        public CaptchaPageView()
        {
            this.InitializeComponent();
			_navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
        }

		protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
		{

		}

		private void SendButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			var vm = this.DataContext as ReplyViewModel;
			if (vm != null)
			{
				vm.Submit.Execute(null);
                _navigationService.GoBack();
			}
		}

    }
}
