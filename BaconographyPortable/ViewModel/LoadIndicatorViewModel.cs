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
        bool _running;
        ISystemServices _systemServices;

        public LoadIndicatorViewModel(IBaconProvider baconProvider)
        {
            _systemServices = baconProvider.GetService<ISystemServices>();
            MessengerInstance.Register<LoadingMessage>(this, OnLoadingMessage);
        }

        bool _progressBarVisibility;
        bool ProgressBarVisibility
        {
            get
            {
                return _progressBarVisibility;
            }
            set
            {
                _progressBarVisibility = value;
                RaisePropertyChanged("ProgressBarVisibility");
            }
        }


        private void OnLoadingMessage(LoadingMessage message)
        {
            if (message.Loading)
            {
                ProgressBarVisibility = true;
                _running = true;
                _dispatcherTimerHandle = _systemServices.StartTimer(OnTick, TimeSpan.FromSeconds(2));
            }
            else
            {
                _running = false;
            }
        }

        private void OnTick(object obj, object obj2)
        {
            if (!_running)
            {
                ProgressBarVisibility = false;
                _systemServices.StopTimer(_dispatcherTimerHandle);
            }
        }
    }
}
