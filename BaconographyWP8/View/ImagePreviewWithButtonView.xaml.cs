using BaconographyPortable.Services;
using Microsoft.Practices.ServiceLocation;
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
    public sealed partial class ImagePreviewWithButtonView : UserControl
    {
        public ImagePreviewWithButtonView()
        {
            this.InitializeComponent();
        }

        private bool showing = false;
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (showing)
            {
                contentControl.Content = null;
                showing = false;
            }
            else
            {
                showing = true;
                contentControl.Content = new PicturePreviewView { DataContext = new BaconographyWP8.Converters.PreviewDataConverter.PreviewImageViewModelWrapper(ServiceLocator.Current.GetInstance<IImagesService>().GetImagesFromUrl("", DataContext as string), ServiceLocator.Current.GetInstance<ISystemServices>()) };
            }
        }
    }
}
