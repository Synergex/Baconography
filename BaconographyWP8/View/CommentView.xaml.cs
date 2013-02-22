
using BaconographyWP8Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BaconographyWP8.View
{
	[ViewUri("/View/CommentView.xaml")]
	public sealed partial class CommentView : UserControl
    {
        public CommentView()
        {
            this.InitializeComponent();
        }
    }
}
