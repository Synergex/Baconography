using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using BaconographyPortable.ViewModel;
using Microsoft.Practices.ServiceLocation;
using BaconographyPortable.Services;
using GalaSoft.MvvmLight.Messaging;
using BaconographyPortable.Messages;
using GalaSoft.MvvmLight.Command;
using BaconographyWP8Core.Common;
using GalaSoft.MvvmLight;
using BaconographyWP8;
using GalaSoft.MvvmLight.Ioc;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace BaconographyWP8Core.View
{
    [ViewUri("/BaconographyWP8Core;component/View/LinkedReadabilityView.xaml")]
    public partial class LinkedReadabilityView : PhoneApplicationPage
    {
        public LinkedReadabilityView()
        {
            InitializeComponent();
        }

        private void DeFocusContent()
        {
            var context = DataContext as ReadableArticleViewModel;
            if (context != null)
                context.ContentIsFocused = false;

            articleView.IsHitTestVisible = false;
            disabledRect.Opacity = 0.35;
            disabledRect.IsHitTestVisible = true;
            Focus();
            appBar.Opacity = 1;
        }

        private void FocusContent()
        {
            var context = DataContext as ReadableArticleViewModel;
            if (context != null)
                context.ContentIsFocused = true;

            articleView.IsHitTestVisible = true;
            disabledRect.IsHitTestVisible = false;
            articleView.Focus();
            disabledRect.Opacity = 0.0;
            appBar.Opacity = .80;
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (ContentFocused && !ServiceLocator.Current.GetInstance<ISettingsService>().OnlyFlipViewImages)
            {
                e.Cancel = true;
                DeFocusContent();
            }
            else
                base.OnBackKeyPress(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            if (e.NavigationMode == NavigationMode.New && e.IsNavigationInitiator)
            {

                var absPath = e.Uri.ToString().Contains('?') ? e.Uri.ToString().Substring(0, e.Uri.ToString().IndexOf("?")) : e.Uri.ToString();
                if (absPath == "/BaconographyWP8Core;component/View/LinkedPictureView.xaml" || absPath == "/BaconographyWP8Core;component/View/LinkedReadabilityView.xaml" ||
                    absPath == "/BaconographyWP8Core;component/View/LinkedSelfTextPageView.xaml")
                {
                    ServiceLocator.Current.GetInstance<INavigationService>().RemoveBackEntry();
                }
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.New && e.Uri.ToString() == "/BaconographyWP8Core;component/MainPage.xaml" && e.IsCancelable)
            {
                e.Cancel = true;
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.NavigationMode == NavigationMode.Back)
			{
                
			}
            else if (e.NavigationMode == NavigationMode.Reset)
            {
                //do nothing we have everything we want already here
            }
            else
            {
                if(SimpleIoc.Default.IsRegistered<ReadableArticleViewModel>())
                {
                    var preloadedDataContext = SimpleIoc.Default.GetInstance<ReadableArticleViewModel>();
                    DataContext = preloadedDataContext;
                    SimpleIoc.Default.Unregister<ReadableArticleViewModel>();

                    if (preloadedDataContext.ContentIsFocused)
                        FocusContent();
                    else
                        DeFocusContent();
                }
                else if (this.NavigationContext.QueryString.ContainsKey("data") && this.NavigationContext.QueryString["data"] != null)
                {
                    var unescapedData = HttpUtility.UrlDecode(this.NavigationContext.QueryString["data"]);
                    try
                    {
                        var argTpl = JsonConvert.DeserializeObject<Tuple<string, string>>(unescapedData);
                        Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });
                        DataContext = await ReadableArticleViewModel.LoadAtLeastOne(ServiceLocator.Current.GetInstance<ISimpleHttpService>(), argTpl.Item1, argTpl.Item2);
                        FocusContent();
                    }
                    finally
                    {
                        Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
                    }
                }
            }
        }

        private bool ContentFocused
        {
            get
            {
                var context = DataContext as ReadableArticleViewModel;
                if (context != null)
                    return context.ContentIsFocused;
                else
                    return false;
            }
        }

        public void myGridGestureListener_Flick(object sender, FlickGestureEventArgs e)
        {
            if (!ContentFocused)
            {
                FlipViewUtility.FlickHandler(sender, e, DataContext as ViewModelBase, this);
            }
        }

        private async void disabledRect_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            //gesture listener crashes if we dont let it process first
            await Task.Yield();
            FocusContent();
        }
    }
}