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
