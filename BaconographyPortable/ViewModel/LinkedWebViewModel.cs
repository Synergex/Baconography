﻿using BaconographyPortable.Messages;
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
    public class LinkedWebViewModel : ViewModelBase
    {
        INavigationService _navigationService;
        IWebViewWrapper _webViewWrapper;
        public LinkedWebViewModel(IBaconProvider baconProvider)
        {
            _navigationService = baconProvider.GetService<INavigationService>();
            _webViewWrapper = baconProvider.GetService<IWebViewWrapper>();
            MessengerInstance.Register<NavigateToUrlMessage>(this, OnNavigateTo);
        }

        public IWebViewWrapper WebViewWrapper
        {
            get
            {
                return _webViewWrapper;
            }
        }

        private void OnNavigateTo(NavigateToUrlMessage message)
        {
            _webViewWrapper.Url = message.TargetUrl;
            LinkedTitle = message.Title;
        }

        public string Source
        {
            get
            {
                return _webViewWrapper.Url;
            }
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

        private RelayCommand _gotoBrowser;
        public RelayCommand GotoBrowser
        {
            get
            {
                if (_gotoBrowser == null)
                {
                    _gotoBrowser = new RelayCommand(() =>
                    {
                        //no reason the leave them on the page they are about to open in a seperate browser
                        _navigationService.GoBack();
                        _navigationService.NavigateToExternalUri(new Uri(_webViewWrapper.Url));
                    });
                }
                return _gotoBrowser;
            }
        }
    }
}
