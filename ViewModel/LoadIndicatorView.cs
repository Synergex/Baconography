using GalaSoft.MvvmLight;
using Baconography.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Baconography.ViewModel
{
    public class LoadIndicatorView : ViewModelBase
    {
        Windows.UI.Xaml.DispatcherTimer _dipatcherTimer = new DispatcherTimer();
        bool _running;
        public LoadIndicatorView()
        {
            //we want the progress bar to display for at least 2 seconds so that isnt not a confusing flash
            _dipatcherTimer.Interval = TimeSpan.FromSeconds(2);
            _dipatcherTimer.Tick += _dipatcherTimer_Tick;
            MessengerInstance.Register<LoadingMessage>(this, message =>
                {
                    if (message.Loading)
                    {
                        ProgressBarVisibility = Visibility.Visible;
                        _running = true;
                        _dipatcherTimer.Start();
                    }
                    else
                    {
                        _running = false;
                    }
                });
        }

        void _dipatcherTimer_Tick(object sender, object e)
        {
            if (!_running)
            {
                ProgressBarVisibility = Visibility.Collapsed;
                _dipatcherTimer.Stop();
            }
        }
        
        private Visibility _progressBarVisibility = Visibility.Collapsed;

        public Visibility ProgressBarVisibility
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
    }
}
