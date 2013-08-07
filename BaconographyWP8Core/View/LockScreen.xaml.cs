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
    [ViewUri("/BaconographyWP8Core;component/View/LockScreen.xaml")]
    public partial class LockScreen : PhoneApplicationPage
    {
        public LockScreen()
        {
            InitializeComponent();
        }
    }
}