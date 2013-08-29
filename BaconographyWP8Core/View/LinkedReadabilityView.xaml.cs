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
                if (this.NavigationContext.QueryString["data"] != null)
                {
                    var unescapedData = HttpUtility.UrlDecode(this.NavigationContext.QueryString["data"]).Trim('\"');
                    try
                    {
                        Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });
                        DataContext = await ReadableArticleViewModel.LoadAtLeastOne(ServiceLocator.Current.GetInstance<ISimpleHttpService>(), unescapedData);
                    }
                    finally
                    {
                        Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
                    }
                }
            }
        }
    }
}