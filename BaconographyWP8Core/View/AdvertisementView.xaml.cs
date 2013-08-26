using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace BaconographyWP8.View
{
    public partial class AdvertisementView : UserControl
    {
        public AdvertisementView()
        {
            InitializeComponent();
        }

        private void AdControl_AdRefreshed(object sender, EventArgs e)
        {

        }

        private void AdControl_ErrorOccurred(object sender, Microsoft.Advertising.AdErrorEventArgs e)
        {
            advertisement.Height = 0;
            advertisement.Visibility = System.Windows.Visibility.Collapsed;
            adDuplexAd.Visibility = System.Windows.Visibility.Visible;
        }
    }
}
