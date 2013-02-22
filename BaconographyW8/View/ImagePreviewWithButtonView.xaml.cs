using BaconographyPortable.Services;
using Microsoft.Practices.ServiceLocation;
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

namespace BaconographyW8.View
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
                if (contentControl.Content != null && contentControl.Content is PicturePreviewView && ((PicturePreviewView)contentControl.Content).DataContext is BaconographyW8.Converters.PreviewDataConverter.PreviewImageViewModelWrapper)
                {
                    ((BaconographyW8.Converters.PreviewDataConverter.PreviewImageViewModelWrapper)((PicturePreviewView)contentControl.Content).DataContext).Cleanup();
                }
                contentControl.Content = null;
                showing = false;
            }
            else
            {
                showing = true;
                contentControl.Content = new PicturePreviewView { DataContext = new BaconographyW8.Converters.PreviewDataConverter.PreviewImageViewModelWrapper(ServiceLocator.Current.GetInstance<IImagesService>().GetImagesFromUrl("", DataContext as string), ServiceLocator.Current.GetInstance<ISystemServices>()) };
            }
        }
    }
}
