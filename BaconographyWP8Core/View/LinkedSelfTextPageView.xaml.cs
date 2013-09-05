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
using BaconographyWP8Core.Common;
using GalaSoft.MvvmLight;
using System.Threading.Tasks;
using Microsoft.Practices.ServiceLocation;
using BaconographyPortable.Services;
using Newtonsoft.Json;
using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;

namespace BaconographyWP8Core.View
{
    [ViewUri("/BaconographyWP8Core;component/View/LinkedSelfTextPageView.xaml")]
    public partial class LinkedSelfTextPageView : PhoneApplicationPage
    {
        public LinkedSelfTextPageView()
        {
            InitializeComponent();
        }

        private void DefocusContent()
        {
            var context = DataContext as LinkViewModel;
            if (context != null)
                context.ContentIsFocused = false;

            selfTextView.IsHitTestVisible = false;
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

            selfTextView.IsHitTestVisible = true;
            disabledRect.IsHitTestVisible = false;
            selfTextView.Focus();
            disabledRect.Opacity = 0.0;
            appBar.Opacity = .80;
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (ContentFocused && !ServiceLocator.Current.GetInstance<ISettingsService>().OnlyFlipViewImages)
            {
                e.Cancel = true;
                DefocusContent();
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

        protected override void OnNavigatedTo(NavigationEventArgs e)
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
                if (!string.IsNullOrWhiteSpace(this.NavigationContext.QueryString["data"]))
                {
                    var unescapedData = HttpUtility.UrlDecode(this.NavigationContext.QueryString["data"]);
                    var deserializedObject = JsonConvert.DeserializeObject<Tuple<Thing, bool>>(unescapedData);
                    if (deserializedObject != null && deserializedObject.Item1.Data is Link)
                    {
                        var vm = new LinkViewModel(deserializedObject.Item1, ServiceLocator.Current.GetInstance<IBaconProvider>(), deserializedObject.Item2);
                        DataContext = vm;
                        if (vm.ContentIsFocused)
                        {
                            FocusContent();
                        }
                        else
                            DefocusContent();
                    }
                }
                else
                {
                    var notificationService = ServiceLocator.Current.GetInstance<INotificationService>();
                    notificationService.CreateNotification("TLDR; something bad happened, send /u/hippiehunter a PM letting us know what you clicked on");
                }
            }
        }
        private bool ContentFocused
        {
            get
            {
                var context = DataContext as LinkViewModel;
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