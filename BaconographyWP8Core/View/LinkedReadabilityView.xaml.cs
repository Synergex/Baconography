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

namespace BaconographyWP8Core.View
{
    [ViewUri("/BaconographyWP8Core;component/View/LinkedReadabilityView.xaml")]
    public partial class LinkedReadabilityView : PhoneApplicationPage
    {
        public LinkedReadabilityView()
        {
            InitializeComponent();
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
                if (this.NavigationContext.QueryString.ContainsKey("data") && this.NavigationContext.QueryString["data"] != null)
                {
                    var unescapedData = HttpUtility.UrlDecode(this.NavigationContext.QueryString["data"]);
                    try
                    {
                        var argTpl = JsonConvert.DeserializeObject<Tuple<string, string>>(unescapedData);
                        Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });
                        DataContext = await ReadableArticleViewModel.LoadAtLeastOne(ServiceLocator.Current.GetInstance<ISimpleHttpService>(), argTpl.Item1, argTpl.Item2);
                    }
                    finally
                    {
                        Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
                    }
                }
                else
                {
                    var preloadedDataContext = SimpleIoc.Default.GetInstance<ReadableArticleViewModel>();
                    DataContext = preloadedDataContext;
                }
            }
        }

        public void myGridGestureListener_Flick(object sender, FlickGestureEventArgs e)
        {
            FlipViewUtility.FlickHandler(sender, e, DataContext as ViewModelBase, this);
        }
    }
}