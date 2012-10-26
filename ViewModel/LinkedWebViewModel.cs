using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Baconography.Common;
using Baconography.Messages;
using Baconography.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Baconography.ViewModel
{
    public class LinkedWebViewModel : ViewModelBase
    {
        INavigationService _navService;
        public LinkedWebViewModel(INavigationService navService)
        {
            _navService = navService;
            MessengerInstance.Register<NavigateToUrlMessage>(this, message =>
            {
                Source = message.TargetUrl;
                LinkedTitle = message.Title;
            });
        }

        private string _linkedTitle;
        public string LinkedTitle
        {
            get
            {
                return _linkedTitle;
            }
            set
            {
                _linkedTitle = value;
                RaisePropertyChanged("LinkedTitle");
            }
        }

        private string _source;
        public string Source
        {
            get
            {
                return _source;
            }
            set
            {
                _source = value;
                RaisePropertyChanged("Source");
            }
        }

        private WebViewWrapper _webView;
        public WebViewWrapper WebView
        {
            get
            {
                return _webView;
            }
            set
            {
                if (_webView != null)
                {
                    _webView.Dispose();
                    _webView = null;
                }

                _webView = value;
            }
        }

        private RelayCommand _gotoBrowser;
        public RelayCommand GotoBrowser
        {
            get
            {
                if (_gotoBrowser == null)
                {
                    _gotoBrowser = new RelayCommand(async () => 
                        {
                            //no reason the leave them on the page they are about to open in a seperate browser
                            _navService.GoBack();
                            await Windows.System.Launcher.LaunchUriAsync(new Uri(WebView.CurrentUrl));
                        });
                }
                return _gotoBrowser;
            }
        }

        private RelayCommand _exit;
        public RelayCommand Exit
        {
            get
            {
                if (_exit == null)
                {
                    _exit = new RelayCommand(() => Application.Current.Exit());
                }
                return _exit;
            }
        }
    }
}
