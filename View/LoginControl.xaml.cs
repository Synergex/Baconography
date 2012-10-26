using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Baconography.View
{
    public sealed partial class LoginControl : UserControl
    {
        public LoginControl()
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
