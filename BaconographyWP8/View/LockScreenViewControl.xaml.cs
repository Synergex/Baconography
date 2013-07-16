using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using BaconographyWP8.ViewModel;

namespace BaconographyWP8.View
{
    public partial class LockScreenViewControl : UserControl
    {
        public LockScreenViewControl()
        {
            InitializeComponent();
        }

        public LockScreenViewControl(LockScreenViewModel lockScreenViewModel)
        {
            InitializeComponent();
            backgroundImage.ImageSource = lockScreenViewModel.ImageSource;
            borderBackground.Opacity = lockScreenViewModel.OverlayOpacity;
            foreach(var item in lockScreenViewModel.OverlayItems)
            {
                itemsControl.Items.Add(new LockScreenOverlayItem(item));
            }
        }
    }
}
