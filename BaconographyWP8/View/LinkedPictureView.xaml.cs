using BaconographyPortable.ViewModel;
using BaconographyWP8Core;
using Microsoft.Phone.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace BaconographyWP8.View
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
	[ViewUri("/View/LinkedPictureView.xaml")]
	public sealed partial class LinkedPictureView : PhoneApplicationPage
    {
        //cheating a little bit here but its for the best
        LinkedPictureViewModel _pictureViewModel;
        public LinkedPictureView()
        {
            this.InitializeComponent();
        }

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			if (this.State != null && this.State.ContainsKey("PictureViewModel"))
			{
				_pictureViewModel = this.State["PictureViewModel"] as LinkedPictureViewModel;
			}
			else if (this.NavigationContext.QueryString["data"] != null)
			{
				var unescapedData = Uri.UnescapeDataString(this.NavigationContext.QueryString["data"]);
				var deserializedObject = JsonConvert.DeserializeObject<IEnumerable<Tuple<string, string>>>(unescapedData);
				if (deserializedObject != null)
				{
					_pictureViewModel = new LinkedPictureViewModel { Pictures = deserializedObject.Select(tpl => new LinkedPictureViewModel.LinkedPicture { Title = tpl.Item1, ImageSource = tpl.Item2 }) };
				}
			}
			DataContext = _pictureViewModel;
		}

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			this.State["PictureViewModel"] = _pictureViewModel;
			Content = null;
			((LinkedPictureViewModel)DataContext).Cleanup();
		}
		
    }
}
