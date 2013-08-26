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
using System.Windows.Media.Imaging;
using System.IO.IsolatedStorage;
using System.IO;

namespace BaconographyWP8BackgroundControls.View
{
    public partial class LockScreenViewControl : UserControl, IDisposable
    {
        public LockScreenViewControl()
        {
            InitializeComponent();
        }

        private static BitmapImage GetImageFromIsolatedStorage(string imageName)
        {
            var bimg = new BitmapImage();
            bimg.CreateOptions = BitmapCreateOptions.None;
            using (var iso = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (var stream = iso.OpenFile(imageName, FileMode.Open, FileAccess.Read))
                {
                    bimg.SetSource(stream);
                }
            }
            return bimg;
        }

        public LockScreenViewControl(LockScreenViewModel lockScreenViewModel)
        {
            InitializeComponent();
            backgroundImage.ImageSource = GetImageFromIsolatedStorage(lockScreenViewModel.ImageSource);
            borderBackground.Opacity = lockScreenViewModel.OverlayOpacity;
            overlayBorder.Margin = lockScreenViewModel.Margin;
            overlayBorder.CornerRadius = lockScreenViewModel.CornerRadius;
            innerBorder.Margin = lockScreenViewModel.InnerMargin;
            if (lockScreenViewModel.NumberOfItems > 0 && lockScreenViewModel.OverlayItems.Count > 0)
            {
                foreach (var item in lockScreenViewModel.OverlayItems)
                {
                    itemsControl.Items.Add(new LockScreenOverlayItem(item));
                }
            }
            else
            {
                itemsControl.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        public void Dispose()
        {
            backgroundImage.ImageSource = null;
            itemsControl.Items.Clear();
        }
    }
}
