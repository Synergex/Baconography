using BaconographyPortable.Services;
using GalaSoft.MvvmLight;
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
            VM = new PictureDataVM();
            VM.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }



        private bool showing = false;
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (showing)
            {
                if (VM.PictureData is BaconographyW8.Converters.PreviewDataConverter.PreviewImageViewModelWrapper)
                {
                    VM.PictureData.Cleanup();
                }
                VM.Visibility = Visibility.Collapsed;
                VM.PictureData = null;
                showing = false;
            }
            else
            {
                showing = true;
                VM.Visibility = Visibility.Visible;
                VM.PictureData = new BaconographyW8.Converters.PreviewDataConverter.PreviewImageViewModelWrapper(ServiceLocator.Current.GetInstance<IImagesService>().GetImagesFromUrl("", DataContext as string), ServiceLocator.Current.GetInstance<ISystemServices>());
            }
        }
        public class PictureDataVM : ViewModelBase
        {
            BaconographyW8.Converters.PreviewDataConverter.PreviewImageViewModelWrapper _pictureData;
            public BaconographyW8.Converters.PreviewDataConverter.PreviewImageViewModelWrapper PictureData
            {
                get
                {
                    return _pictureData;
                }
                set
                {
                    _pictureData = value;
                    RaisePropertyChanged("PictureData");
                }
            }
            Visibility _visibility;
            public Visibility Visibility
            {
                get
                {
                    return _visibility;
                }
                set
                {
                    _visibility = value;
                    RaisePropertyChanged("Visibility");
                }
            }
        }

        public PictureDataVM VM { get; set; }
    }
}
