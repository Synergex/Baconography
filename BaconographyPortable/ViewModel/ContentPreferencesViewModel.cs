using BaconographyPortable.Services;
using GalaSoft.MvvmLight;
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
    }
}
