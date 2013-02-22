using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
using BaconographyW8.View;
using DXRenderInterop;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace BaconographyW8.Converters
{
    public class PreviewDataConverter : IValueConverter
    {
        IImagesService _imagesService;
        ISystemServices _systemServices;
        public PreviewDataConverter(IBaconProvider baconProvider)
        {
            _imagesService = baconProvider.GetService<IImagesService>();
            _systemServices = baconProvider.GetService<ISystemServices>();
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var tpl = value as Tuple<bool, string>;
            if (tpl.Item1)
                return new PicturePreviewView { DataContext = new PreviewImageViewModelWrapper(_imagesService.GetImagesFromUrl("", tpl.Item2), _systemServices) };
            else
                return null;

        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
        public class PreviewImageViewModelWrapper : ViewModelBase
        {
            List<Tuple<string, string>> _finishedImages;
            Dictionary<int, ImageSource> _imageSources;
            ISystemServices _systemServices;
            public PreviewImageViewModelWrapper(Task<IEnumerable<Tuple<string, string>>> imagesTask, ISystemServices systemServices)
            {
                _imageSources = new Dictionary<int, Windows.UI.Xaml.Media.ImageSource>();
                _systemServices = systemServices;
                IsLoading = true;
                imagesTask.ContinueWith(FinishLoad, TaskScheduler.FromCurrentSynchronizationContext());
                MoveBack = new RelayCommand(() => CurrentPosition = _currentPosition - 1);
                MoveForward = new RelayCommand(() => CurrentPosition = _currentPosition + 1);
            }

            public override void Cleanup()
            {
                base.Cleanup();
                foreach (GifRenderer img in _imageSources.Values)
                    img.Visible = false;
                _imageSources.Clear();
            }

            private async Task<byte[]> DownloadImageFromWebsiteAsync(string url)
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                    using (WebResponse response = await request.GetResponseAsync())
                    {
                        using (Stream imageStream = response.GetResponseStream())
                        {
                            using (var result = new MemoryStream())
                            {
                                await imageStream.CopyToAsync(result);
                                return result.ToArray();
                            }
                        }
                    }
                }
                catch (WebException ex)
                {
                    return null;
                }
            }

            private async void FinishLoad(Task<IEnumerable<Tuple<string, string>>> imagesTask)
            {
                _finishedImages = new List<Tuple<string, string>>(imagesTask.Result);

                for(int i = 0; i < _finishedImages.Count; i++)
                {
                    //if (_finishedImages[i].Item2.EndsWith(".gif"))
                    {
                        var renderer = GifRenderer.CreateGifRenderer(await DownloadImageFromWebsiteAsync(_finishedImages[i].Item2));
                        if(renderer != null)
                            _imageSources.Add(i, renderer);
                    }
                }
                


                IsLoading = false;
                RaisePropertyChanged("ImageSource");
                RaisePropertyChanged("IsLoading");
                RaisePropertyChanged("IsAlbum");
                RaisePropertyChanged("AlbumSize");
                CurrentPosition = 0;
            }

            public int AlbumSize
            {
                get
                {
                    return _finishedImages != null && _finishedImages.Count > _currentPosition ? _finishedImages.Count : 0;
                }
            }

            public bool IsAlbum
            {
                get
                {
                    return _finishedImages != null && _finishedImages.Count > _currentPosition ? _finishedImages.Count > 1 : false;
                }
            }

            public string Title
            {
                get
                {
                    return _finishedImages != null && _finishedImages.Count > _currentPosition ? _finishedImages[_currentPosition].Item1 : "";
                }
            }

            public object ImageSource
            {
                get
                {
                    if (_imageSources.ContainsKey(_currentPosition))
                    {
                        return _imageSources[_currentPosition] ;
                    }
                    else
                    {
                        var result = _finishedImages != null && _finishedImages.Count > _currentPosition ? _finishedImages[_currentPosition].Item2 : "";
                        return result;
                    }
                }
            }

            public bool IsLoading { get; set; }

            private int _currentPosition;
            public int CurrentPosition
            {
                get
                {
                    return _currentPosition + 1;
                }
                set
                {
                    if (_imageSources.ContainsKey(_currentPosition))
                    {
                        ((GifRenderer)_imageSources[_currentPosition]).Visible = false;
                    }

                    if (value >= _finishedImages.Count)
                        _currentPosition = 0;
                    else if (value < 0)
                        _currentPosition = _finishedImages.Count - 1;
                    else
                        _currentPosition = value;

                    if (_imageSources.ContainsKey(_currentPosition))
                    {
                        ((GifRenderer)_imageSources[_currentPosition]).Visible = true;
                    }

                    RaisePropertyChanged("CurrentPosition");
                    RaisePropertyChanged("Title");
                    RaisePropertyChanged("Url");
                }
            }

            public RelayCommand MoveBack { get; set; }
            public RelayCommand MoveForward { get; set; }
        }
    }
}
