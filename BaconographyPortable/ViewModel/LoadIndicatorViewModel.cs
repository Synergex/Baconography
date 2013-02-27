using BaconographyPortable.Messages;
using BaconographyPortable.Services;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel
{
    public class LoadIndicatorViewModel : ViewModelBase
    {
        object _dispatcherTimerHandle;
        int _running;
        ISystemServices _systemServices;

        public LoadIndicatorViewModel(IBaconProvider baconProvider)
        {
            _running = 0;
            _systemServices = baconProvider.GetService<ISystemServices>();
            MessengerInstance.Register<LoadingMessage>(this, OnLoadingMessage);
        }

        bool _progressBarVisibility;
        public bool ProgressBarVisibility
        {
            get
            {
                return _progressBarVisibility;
            }
            set
            {
                _progressBarVisibility = value;
                try
                {
                    RaisePropertyChanged("ProgressBarVisibility");
                }
                catch
                {
                    //this sometimes goes weird if we're not in the main application (search/picker)
                }
            }
        }

        private void OnLoadingMessage(LoadingMessage message)
        {
            if (message.Loading)
            {
                ProgressBarVisibility = true;
                _running++;
                _dispatcherTimerHandle = _systemServices.StartTimer(OnTick, TimeSpan.FromSeconds(2), true);
            }
            else
            {
                _running--;
            }
        }

        private void OnTick(object obj, object obj2)
        {
            if (_running == 0)
            {
                ProgressBarVisibility = false;
				_systemServices.StopTimer(obj);
            }
        }
    }
}
