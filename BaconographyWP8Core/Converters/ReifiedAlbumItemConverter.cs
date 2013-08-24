using BaconographyPortable.Messages;
using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
using BaconographyWP8.PlatformServices;
using BaconographyWP8.View;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using ImageTools.IO.Gif;
using Microsoft.Phone.Controls;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace BaconographyWP8.Converters
{
    public class ReifiedAlbumItemConverter : IValueConverter
    {
        public static CancellationTokenSource CancelSource = new CancellationTokenSource();
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ObservableCollection<PivotItem> boundControls = new ObservableCollection<PivotItem>();
            AddInitial(value as IEnumerable<ViewModelBase>, boundControls);

            return boundControls;
        }

        async void AddInitial(IEnumerable<ViewModelBase> viewModels, ObservableCollection<PivotItem> boundControls)
        {
            var cancelToken = CancelSource.Token;
            if(viewModels != null)
            {
                foreach (var viewModel in viewModels)
                {
                    var result = await MapViewModel(viewModel);

                    if (cancelToken.IsCancellationRequested)
                        break;

                    if(result != null)
                        boundControls.Add(result);
                }
            }
        }

        async Task<PivotItem> MapViewModel(ViewModelBase viewModel)
        {
            var cancelToken = CancelSource.Token;
            var rvm = viewModel as LinkedPictureViewModel.LinkedPicture;
            string domain = rvm.Url;
            try
            {
                var uri = new Uri(rvm.Url);
                domain = uri.Host;
            }
            catch{}


            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true, Percentage = 0, Message = "loading from " + domain });
            try
            {
                var imageBytes = await SimpleHttpService.GetBytesWithProgress(cancelToken, rvm.Url, (progress) => 
                    {
                        Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true, Percentage = progress, Message = "loading from " + domain });
                    });
                Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });

                bool isGif = false;
                if (imageBytes != null && imageBytes.Length >= 6)
                {
                    isGif =
                        imageBytes[0] == 0x47 && // G
                        imageBytes[1] == 0x49 && // I
                        imageBytes[2] == 0x46 && // F
                        imageBytes[3] == 0x38 && // 8
                       (imageBytes[4] == 0x39 || imageBytes[4] == 0x37) && // 9 or 7
                        imageBytes[5] == 0x61;   // a
                }
                else
                {
                    ServiceLocator.Current.GetInstance<INotificationService>().CreateNotification("failed to load image: unknown");
                    return null;
                }

                rvm.ImageSource = imageBytes;
                rvm.IsGif = isGif;
            }
            catch (Exception ex)
            {
                if (!(ex is TaskCanceledException))
                    ServiceLocator.Current.GetInstance<INotificationService>().CreateNotification("failed to load image: " + ex.Message);
                else
                {
                    Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
                }
            }

            return new PivotItem { DataContext = viewModel };
        }


        public static UIElement MapPictureVM(ViewModelBase vm)
        {
            var linkedPicture = vm as LinkedPictureViewModel.LinkedPicture;
            if (linkedPicture != null)
            {
                if (linkedPicture.IsGif)
                {
                    return new ScalingGifView { DataContext = linkedPicture, ImageSource = linkedPicture.ImageSource };
                }
                else
                {
                    return new ScalingPictureView { DataContext = linkedPicture, ImageSource = linkedPicture.ImageSource };
                }
            }
            return null;
        }



        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
