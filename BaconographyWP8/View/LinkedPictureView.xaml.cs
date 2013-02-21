using BaconographyPortable.ViewModel;
using BaconographyWP8Core;
using Microsoft.Phone.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
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

		private void Image_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
		{
			var image = sender as Image;
			if (image == null)
				return;
			if (e.PinchManipulation != null)
			{
				var transform = (CompositeTransform)image.RenderTransform;

				// Scale Manipulation
				transform.ScaleX = e.PinchManipulation.CumulativeScale;
				transform.ScaleY = e.PinchManipulation.CumulativeScale;

				// Translate manipulation
				var originalCenter = e.PinchManipulation.Original.Center;
				var newCenter = e.PinchManipulation.Current.Center;
				transform.TranslateX = newCenter.X - originalCenter.X;
				transform.TranslateY = newCenter.Y - originalCenter.Y;

				// Rotation manipulation
				/*transform.Rotation = angleBetween2Lines(
					e.PinchManipulation.Current,
					e.PinchManipulation.Original);*/

				// end 
				e.Handled = true;
			}
		}


		// copied from http://www.developer.nokia.com/Community/Wiki/Real-time_rotation_of_the_Windows_Phone_8_Map_Control
		/*
		public static double angleBetween2Lines(PinchContactPoints line1, PinchContactPoints line2)
		{
			if (line1 != null && line2 != null)
			{
				double angle1 = Math.Atan2(line1.PrimaryContact.Y - line1.SecondaryContact.Y,
										   line1.PrimaryContact.X - line1.SecondaryContact.X);
				double angle2 = Math.Atan2(line2.PrimaryContact.Y - line2.SecondaryContact.Y,
										   line2.PrimaryContact.X - line2.SecondaryContact.X);
				return (angle1 - angle2) * 180 / Math.PI;
			}
			else { return 0.0; }
		}*/

		
    }
}
