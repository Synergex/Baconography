
using BaconographyPortable.ViewModel;
using BaconographyWP8.Messages;
using BaconographyWP8Core;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Windows.Foundation;
using Windows.Foundation.Collections;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BaconographyWP8.View
{
    [ViewUri("/BaconographyWP8Core;component/View/CommentView.xaml")]
	public sealed partial class CommentView : UserControl
    {
        public CommentView()
        {
            this.InitializeComponent();
        }

        private bool IsParentButton(UIElement element)
        {
            if (element == null)
                return false;
            if (element is Button)
                return true;

            return IsParentButton(VisualTreeHelper.GetParent(element) as UIElement);
        }

		private void Comment_Hold(object sender, System.Windows.Input.GestureEventArgs e)
		{
            if (!e.Handled && !IsParentButton(e.OriginalSource as FrameworkElement) )
            {
                var commentVm = this.DataContext as CommentViewModel;
                if (commentVm != null)
                    commentVm.ShowExtendedView.Execute(null);
            }
		}
    }
}
