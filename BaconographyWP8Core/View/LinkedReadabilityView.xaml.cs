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

namespace BaconographyWP8Core.View
{
    [ViewUri("/BaconographyWP8Core;component/View/LinkedReadabilityView.xaml")]
    public partial class LinkedReadabilityView : PhoneApplicationPage
    {
        public LinkedReadabilityView()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (this.NavigationContext.QueryString["data"] != null)
			{
				var unescapedData = HttpUtility.UrlDecode(this.NavigationContext.QueryString["data"]).Trim('\"');
                DataContext = await ReadableArticleViewModel.LoadAtLeastOne(ServiceLocator.Current.GetInstance<ISimpleHttpService>(), unescapedData);
            }
        }
    }
}