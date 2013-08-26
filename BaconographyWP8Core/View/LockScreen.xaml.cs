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
using System.Windows.Data;
using System.Windows.Media;
using BaconographyWP8Core.ViewModel;
using Microsoft.Practices.ServiceLocation;
using BaconographyPortable.Services;

namespace BaconographyWP8.View
{
    [ViewUri("/BaconographyWP8Core;component/View/LockScreen.xaml")]
    public partial class LockScreen : PhoneApplicationPage
    {
        PreviewLockScreenViewModel _plsvm;

        public LockScreen()
        {
            InitializeComponent();

            if (LayoutRoot != null && LayoutRoot.DataContext != null)
            {
                _plsvm = LayoutRoot.DataContext as PreviewLockScreenViewModel;
            }

            this.UpdateLayout();
        }

        private void OverlayItemCount_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_plsvm != null)
                _plsvm.NumberOfItems = (int)(sender as Slider).Value;
        }

        private void OverlayOpacity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_plsvm != null)
                _plsvm.OverlayOpacity = (float)((sender as Slider).Value / 100);
        }

        private void RoundedCorners_Changed(object sender, RoutedEventArgs e)
        {
            if (_plsvm != null)
            {
                _plsvm.RoundedCorners = (bool)((CheckBox)sender).IsChecked;
                this.InvalidateMeasure();
                this.InvalidateArrange();
                this.UpdateLayout();
            }
        }

        private void ShowMessages_Changed(object sender, RoutedEventArgs e)
        {
            if (_plsvm != null)
            {
                _plsvm.ShowMessages = (bool)((CheckBox)sender).IsChecked;
            }
        }

        private void ShowTopPosts_Changed(object sender, RoutedEventArgs e)
        {
            if (_plsvm != null)
            {
                _plsvm.ShowTopPosts = (bool)((CheckBox)sender).IsChecked;
            }
        }

        private void Finished_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
            navigationService.GoBack();
        }
    }
}