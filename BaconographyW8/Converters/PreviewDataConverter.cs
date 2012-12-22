using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
using BaconographyW8.View;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

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
            ISystemServices _systemServices;
            public PreviewImageViewModelWrapper(Task<IEnumerable<Tuple<string, string>>> imagesTask, ISystemServices systemServices)
            {
                _systemServices = systemServices;
                IsLoading = true;
                imagesTask.ContinueWith(FinishLoad, TaskScheduler.FromCurrentSynchronizationContext());
                MoveBack = new RelayCommand(() => CurrentPosition = _currentPosition - 1);
                MoveForward = new RelayCommand(() => CurrentPosition = _currentPosition + 1);
            }

            private void FinishLoad(Task<IEnumerable<Tuple<string, string>>> imagesTask)
            {
                _finishedImages = new List<Tuple<string, string>>(imagesTask.Result);
                IsLoading = false;
                RaisePropertyChanged("IsLoading");
                RaisePropertyChanged("IsAlbum");
                RaisePropertyChanged("AlbumSize");
                CurrentPosition = 0;
            }

            public int AlbumSize
            {
                get
                {
                    return _finishedImages != null ? _finishedImages.Count : 0;
                }
            }

            public bool IsAlbum
            {
                get
                {
                    return _finishedImages != null ? _finishedImages.Count > 1 : false;
                }
            }

            public string Title
            {
                get
                {
                    return _finishedImages != null ? _finishedImages[_currentPosition].Item1 : "";
                }
            }

            public string Url
            {
                get
                {
                    return _finishedImages != null ? _finishedImages[_currentPosition].Item2 : "";
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
                    if (value >= _finishedImages.Count)
                        _currentPosition = 0;
                    else if (value < 0)
                        _currentPosition = _finishedImages.Count - 1;
                    else
                        _currentPosition = value;

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
