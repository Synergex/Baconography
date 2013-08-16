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
            var rvm = viewModel as LinkedPictureViewModel.LinkedPicture;

            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });
            try
            {
                var request = HttpWebRequest.CreateHttp(rvm.Url);
                byte[] result = null;
                using (var response = (await SimpleHttpService.GetResponseAsync(request)))
                {
                    if (response != null)
                    {
                        result = await Task<byte[]>.Run(() =>
                        {
                            byte[] buffer = new byte[11];
                            var stream = response.GetResponseStream();
                            if (stream == null)
                                return null;
                            stream.Read(buffer, 0, 11);
                            return buffer;
                        });
                    }
                }
                Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });

                GifDecoder decoder = new GifDecoder();
                if (result != null && decoder.IsSupportedFileFormat(result))
                {
                    rvm.IsGif = true;
                }
                else if (result != null)
                {
                    rvm.IsGif = false;
                }
                else
                {
                    ServiceLocator.Current.GetInstance<INotificationService>().CreateNotification("failed to load image: unknown");
                    return null;
                }
            }
            catch (Exception ex)
            {
                ServiceLocator.Current.GetInstance<INotificationService>().CreateNotification("failed to load image: " + ex.Message);
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
                    return new ScalingGifView { DataContext = linkedPicture, ImageSource = linkedPicture.Url };
                }
                else
                {
                    return new ScalingPictureView { DataContext = linkedPicture, ImageSource = linkedPicture.Url };
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
