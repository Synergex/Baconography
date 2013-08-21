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

            /*SetValue(ImageSourceProperty, _lsvm.ImageSource);
            SetValue(OverlayItemsProperty, _lsvm.OverlayItems);
            SetValue(OverlayOpacityProperty, _lsvm.OverlayOpacity);
            SetValue(OverlayMarginProperty, _lsvm.Margin as Thickness?);
            SetValue(OverlayInnerMarginProperty, _lsvm.InnerMargin as Thickness?);
            SetValue(CornerRadiusProperty, _lsvm.CornerRadius as CornerRadius?);*/
        }

/*
        #region ImageSource
        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register(
                "ImageSource",
                typeof(string),
                typeof(LockScreen),
                new PropertyMetadata("")
            );
        #endregion

        #region Corner Radius
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                "CornerRadius",
                typeof(CornerRadius),
                typeof(LockScreen),
                new PropertyMetadata(new CornerRadius(0))
            );
        #endregion

        public static readonly DependencyProperty OverlayMarginProperty =
            DependencyProperty.Register(
                "OverlayMargin",
                typeof(Thickness),
                typeof(LockScreen),
                new PropertyMetadata(new Thickness(-5,40,-5,0))
            );

        public static readonly DependencyProperty OverlayInnerMarginProperty =
            DependencyProperty.Register(
                "OverlayInnerMargin",
                typeof(Thickness),
                typeof(LockScreen),
                new PropertyMetadata(new Thickness(17, 0, 17, 0))
            );

        public static readonly DependencyProperty OverlayItemsProperty =
            DependencyProperty.Register(
                "OverlayItems",
                typeof(List<LockScreenMessage>),
                typeof(LockScreen),
                new PropertyMetadata(new List<LockScreenMessage>())
            );

        public static readonly DependencyProperty OverlayOpacityProperty =
            DependencyProperty.Register(
                "OverlayOpacity",
                typeof(float),
                typeof(LockScreen),
                new PropertyMetadata((float).4)
            );
 */

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
    }
}