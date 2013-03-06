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

namespace BaconographyWP8.View
{
	[ViewUri("/View/LoginPageView.xaml")]
	public partial class LoginPageView : PhoneApplicationPage
	{
		public LoginPageView()
		{
			InitializeComponent();
		}
	}
}