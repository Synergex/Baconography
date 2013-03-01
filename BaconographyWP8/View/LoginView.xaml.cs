using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BaconographyWP8.View
{
    public sealed partial class LoginView : UserControl
    {
        public LoginView()
        {
            this.InitializeComponent();
        }

        //hack for visual state managment so we can watermark the passwordbox
        private void PasswordBox_GotFocus_1(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(passwordBox, "WatermarkHidden", true);
        }

        private void PasswordBox_LostFocus_1(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(passwordBox.Password))
            {
                VisualStateManager.GoToState(passwordBox, "WatermarkVisible", true);
            }
        }

        private void passwordBox_Loaded_1(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(passwordBox.Password))
            {
                VisualStateManager.GoToState(passwordBox, "WatermarkVisible", true);
            }
        }
    }
}
