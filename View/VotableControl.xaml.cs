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
    public sealed partial class VotableControl : UserControl
    {
        public VotableControl()
        {
            this.InitializeComponent();
        }

        private void ToggleButton_Checked_1(object sender, RoutedEventArgs e)
        {

        }

		public new Brush Background
		{
			get { return (Brush)GetValue(BackgroundProperty); }
			set
			{
				SetValue(BackgroundProperty, value);
				GridView.Background = value;
			}
		}

		public new static readonly DependencyProperty BackgroundProperty =
			DependencyProperty.Register("Background", typeof(Brush), typeof(VotableControl), new PropertyMetadata("#464646"));
    }
}
