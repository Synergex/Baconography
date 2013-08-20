using BaconographyPortable.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel
{
    public class ContentPreferencesViewModel : ViewModelBase
    {
        IBaconProvider _baconProvider;
        ISettingsService _settingsService;

        public ContentPreferencesViewModel(IBaconProvider baconProvider)
        {
            _baconProvider = baconProvider;
            _settingsService = baconProvider.GetService<ISettingsService>();
        }

		public bool LeftHandedMode
		{
			get
			{
				return _settingsService.LeftHandedMode;
			}
			set
			{
				_settingsService.LeftHandedMode = value;
				RaisePropertyChanged("LeftHandedMode");
			}
		}

		public string Orientation
		{
			get
			{
				return _settingsService.Orientation;
			}
			set
			{
				_settingsService.Orientation = value;
				RaisePropertyChanged("Orientation");
			}
		}

		public bool OrientationLock
		{
			get
			{
				return _settingsService.OrientationLock;
			}
			set
			{
				_settingsService.OrientationLock = value;
				RaisePropertyChanged("OrientationLock");
			}
		}

        public bool TapForComments
        {
            get
            {
                return _settingsService.TapForComments;
            }
            set
            {
                _settingsService.TapForComments = value;
                RaisePropertyChanged("TapForComments");
            }
        }

        public bool AllowNSFWContent
        {
            get
            {
                return _settingsService.AllowOver18;
            }
            set
            {
                _settingsService.AllowOver18 = value;
                RaisePropertyChanged("AllowNSFWContent");
            }
        }

        public bool OfflineOnlyGetsFirstSet
        {
            get
            {
                return _settingsService.OfflineOnlyGetsFirstSet;
            }
            set
            {
                _settingsService.OfflineOnlyGetsFirstSet = value;
                RaisePropertyChanged("OfflineOnlyGetsFirstSet");
            }
        }

        
        public int MaxTopLevelOfflineComments
        {
            get
            {
                return _settingsService.MaxTopLevelOfflineComments;
            }
            set
            {
                _settingsService.MaxTopLevelOfflineComments = value;
                RaisePropertyChanged("MaxTopLevelOfflineComments");
            }
        }


        public string LockScreenReddit
        {
            get
            {
                return _settingsService.LockScreenReddit;
            }
            set
            {
                _settingsService.LockScreenReddit = value;
                RaisePropertyChanged("LockScreenReddit");
            }
        }

        public string LiveTileReddit
        {
            get
            {
                return _settingsService.LiveTileReddit;
            }
            set
            {
                _settingsService.LiveTileReddit = value;
                RaisePropertyChanged("LiveTileReddit");
            }
        }

        public string ImagesSubreddit
        {
            get
            {
                return _settingsService.ImagesSubreddit;
            }
            set
            {
                _settingsService.ImagesSubreddit = value;
                RaisePropertyChanged("ImagesSubreddit");
            }
        }

        public bool HighresLockScreenOnly
        {
            get
            {
                return _settingsService.HighresLockScreenOnly;
            }
            set
            {
                _settingsService.HighresLockScreenOnly = value;
                RaisePropertyChanged("HighresLockScreenOnly");
            }
        }

        public bool EnableUpdates
        {
            get
            {
                return _settingsService.EnableUpdates;
            }
            set
            {
                _settingsService.EnableUpdates = value;
                RaisePropertyChanged("EnableUpdates");
            }
        }

        public bool EnableOvernightUpdates
        {
            get
            {
                return _settingsService.EnableOvernightUpdates;
            }
            set
            {
                _settingsService.EnableOvernightUpdates = value;
                RaisePropertyChanged("EnableOvernightUpdates");
            }
        }

        public bool UpdateOverlayOnlyOnWifi
        {
            get
            {
                return _settingsService.UpdateOverlayOnlyOnWifi;
            }
            set
            {
                _settingsService.UpdateOverlayOnlyOnWifi = value;
                RaisePropertyChanged("UpdateOverlayOnlyOnWifi");
            }
        }

        public bool UpdateImagesOnlyOnWifi
        {
            get
            {
                return _settingsService.UpdateImagesOnlyOnWifi;
            }
            set
            {
                _settingsService.UpdateImagesOnlyOnWifi = value;
                RaisePropertyChanged("UpdateImagesOnlyOnWifi");
            }
        }

        public bool MessagesInLockScreenOverlay
        {
            get
            {
                return _settingsService.MessagesInLockScreenOverlay;
            }
            set
            {
                _settingsService.MessagesInLockScreenOverlay = value;
                RaisePropertyChanged("MessagesInLockScreen");
            }
        }

        public int OverlayOpacity
        {
            get
            {
                return _settingsService.OverlayOpacity;
            }
            set
            {
                _settingsService.OverlayOpacity = value;
                RaisePropertyChanged("OverlayOpacity");
            }
        }


        public bool PostsInLockScreenOverlay
        {
            get
            {
                return _settingsService.PostsInLockScreenOverlay;
            }
            set
            {
                _settingsService.PostsInLockScreenOverlay = value;
                RaisePropertyChanged("PostsInLockScreenOverlay");
            }
        }

        public int OverlayItemCount
        {
            get
            {
                return _settingsService.OverlayItemCount;
            }
            set
            {
                _settingsService.OverlayItemCount = value;
                RaisePropertyChanged("OverlayItemCount");
            }
        }

        public bool AllowPredictiveOfflining
        {
            get
            {
                return _settingsService.AllowPredictiveOfflining;
            }
            set
            {
                _settingsService.AllowPredictiveOfflining = value;
                RaisePropertyChanged("AllowPredictiveOfflining");
            }
        }

        public bool AllowPredictiveOffliningOnMeteredConnection
        {
            get
            {
                return _settingsService.AllowPredictiveOffliningOnMeteredConnection;
            }
            set
            {
                _settingsService.AllowPredictiveOffliningOnMeteredConnection = value;
                RaisePropertyChanged("AllowPredictiveOffliningOnMeteredConnection");
            }
        }

        public int OfflineCacheDays
        {
            get
            {
                return _settingsService.OfflineCacheDays;
            }
            set
            {
                _settingsService.OfflineCacheDays = value;
                RaisePropertyChanged("OfflineCacheDays");
            }
        }

        public bool AllowAdvertising
        {
            get
            {
                return _settingsService.AllowAdvertising;
            }
            set
            {
                _settingsService.AllowAdvertising = value;
                RaisePropertyChanged("AllowAdvertising");
            }
        }


        public bool UseImagePickerForLockScreen
        {
            get
            {
                return _settingsService.UseImagePickerForLockScreen;
            }
            set
            {
                _settingsService.UseImagePickerForLockScreen = value;
                RaisePropertyChanged("UseImagePickerForLockScreen");
            }
        }

        public bool RoundedLockScreen
        {
            get
            {
                return _settingsService.RoundedLockScreen;
            }
            set
            {
                _settingsService.RoundedLockScreen = value;
                RaisePropertyChanged("RoundedLockScreen");
            }
        }

        public RelayCommand ClearOffline
        {
            get
            {
                return new RelayCommand(async () =>
                    {
                        await _baconProvider.GetService<IOfflineService>().Clear();
                    });
            }
        }
    }
}
