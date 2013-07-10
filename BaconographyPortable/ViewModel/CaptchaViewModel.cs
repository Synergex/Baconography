using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using BaconographyPortable.ViewModel.Collections;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel
{
    public class CaptchaViewModel : ViewModelBase
    {
        public static CaptchaViewModel _instance;
        public static CaptchaViewModel GetInstance(IBaconProvider baconProvider)
        {
            if (_instance == null)
                _instance = new CaptchaViewModel(baconProvider);

            return _instance;
        }

        protected IBaconProvider _baconProvider;
        protected INavigationService _navigationService;
		protected ISettingsService _settingsService;
        protected IRedditService _redditService;
        protected IDynamicViewLocator _locatorService;
        public CaptchaViewModel(IBaconProvider baconProvider)
        {
            _baconProvider = baconProvider;
            _navigationService = baconProvider.GetService<INavigationService>();
            _settingsService = baconProvider.GetService<ISettingsService>();
            _redditService = baconProvider.GetService<IRedditService>();
            _locatorService = baconProvider.GetService<IDynamicViewLocator>();
            _send = new RelayCommand(SendImpl);
        }

        public void ShowCaptcha(string iden)
        {
            _captchaIdentifier = iden;
            CaptchaResponse = "";
            ImageSource = "http://www.reddit.com/captcha/" + iden;
            _navigationService.Navigate(_locatorService.CaptchaView, this);
        }

        private string _imageSource;
        public string ImageSource
        {
            get
            {
                return _imageSource;
            }
            set
            {
                _imageSource = value;
                RaisePropertyChanged("ImageSource");
            }
        }

        private string _captchaIdentifier;

        private string _captchaResponse;
        public string CaptchaResponse
        {
            get
            {
                return _captchaResponse;
            }
            set
            {
                _captchaResponse = value;
                RaisePropertyChanged("CaptchaResponse");
                RaisePropertyChanged("CanSend");
            }
        }

        public bool CanSend
        {
            get
            {
                return _captchaResponse.Length >= 6;
            }
        }

        public RelayCommand Send { get { return _send; } }
        private RelayCommand _send;
        private async void SendImpl()
        {
            await _redditService.SubmitCaptcha(CaptchaResponse);
            _navigationService.GoBack();
        }
    }
}
