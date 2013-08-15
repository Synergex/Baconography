using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
using BaconographyWP8.Common;
using BaconographyWP8Core;
using BaconographyWP8Core.Common;
using GalaSoft.MvvmLight;
using Microsoft.Phone.Controls;
using Microsoft.Practices.ServiceLocation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace BaconographyWP8.View
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    [ViewUri("/BaconographyWP8Core;component/View/LinkedPictureView.xaml")]
	public sealed partial class LinkedPictureView : PhoneApplicationPage
    {
        //cheating a little bit here but its for the best
		string _pictureData;
		LinkedPictureViewModel _pictureViewModel;
		PivotItem _currentItem;
        IViewModelContextService _viewModelContextService;
        ISmartOfflineService _smartOfflineService;
        public LinkedPictureView()
        {
            using (ServiceLocator.Current.GetInstance<ISuspendableWorkQueue>().HighValueOperationToken)
            {
                this.InitializeComponent();
            }
			_imageOrigins = new Dictionary<object, string>();
            _viewModelContextService = ServiceLocator.Current.GetInstance<IViewModelContextService>();
            _smartOfflineService = ServiceLocator.Current.GetInstance<ISmartOfflineService>();
            
        }


		private Dictionary<object, string> _imageOrigins;

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			if (this.State != null && this.State.ContainsKey("PictureViewModelData"))
			{
				_pictureData = this.State["PictureViewModelData"] as string;
				if (_pictureData != null)
				{
                    var deserializedObject = JsonConvert.DeserializeObject<Tuple<string, IEnumerable<Tuple<string, string>>, string>>(_pictureData);
                    if (deserializedObject != null)
                    {
                        _pictureViewModel = new LinkedPictureViewModel 
                        { 
                            LinkTitle = deserializedObject.Item1.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&apos;", "'").Trim(),
                            LinkId = deserializedObject.Item3,
                            Pictures = deserializedObject.Item2.Select(tpl => new LinkedPictureViewModel.LinkedPicture 
                            { 
                                Title = tpl.Item1.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&apos;", "'").Trim(), 
                                ImageSource = tpl.Item2, Url = tpl.Item2 
                            }) 
                        };
                    }
				}
			}
			else if (this.NavigationContext.QueryString["data"] != null)
			{
				var unescapedData = HttpUtility.UrlDecode(this.NavigationContext.QueryString["data"]);
                var deserializedObject = JsonConvert.DeserializeObject<Tuple<string, IEnumerable<Tuple<string, string>>, string>>(unescapedData);
				if (deserializedObject != null)
				{
                    _pictureViewModel = new LinkedPictureViewModel 
                    { 
                        LinkTitle = deserializedObject.Item1.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&apos;", "'").Trim(), 
                        LinkId = deserializedObject.Item3,
                        Pictures = deserializedObject.Item2.Select(tpl => new LinkedPictureViewModel.LinkedPicture 
                        { 
                            Title = tpl.Item1.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&apos;", "'").Trim(), 
                            ImageSource = tpl.Item2, Url = tpl.Item2 
                        }) 
                    };
					_pictureData = unescapedData;
				}
			}
			if (DataContext == null || e == null)
				DataContext = _pictureViewModel;

            
            _viewModelContextService.PushViewModelContext(DataContext as ViewModelBase);
            _smartOfflineService.NavigatedToView(typeof(LinkedPictureView), e == null ? true : e.NavigationMode == NavigationMode.New);
            
		}

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.New && e.Uri.ToString() == "/BaconographyWP8Core;component/MainPage.xaml" && e.IsCancelable)
            {
                OnNavigatedTo(null);
                e.Cancel = true;
            }
            _viewModelContextService.PopViewModelContext(DataContext as ViewModelBase);
        }

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
            if(e.NavigationMode == NavigationMode.Back)
                CleanupImageSource();

            if (e.NavigationMode == NavigationMode.New)
            {
                CleanupImageSource();
                ServiceLocator.Current.GetInstance<INavigationService>().RemoveBackEntry();
            }
		}

        private void CleanupImageSource()
        {
            try
            {
                this.State["PictureViewModelData"] = _pictureData;
                //Content = null;
                if (_currentItem != null)
                {
                    var context = _currentItem.DataContext as BaconographyPortable.ViewModel.LinkedPictureViewModel.LinkedPicture;

                    if (context.ImageSource is string)
                    {
                        context.ImageSource = null;
                    }
                    context = null;
                    _currentItem = null;
                }
                ((LinkedPictureViewModel)DataContext).Cleanup();
            }
            catch (Exception ex)
            {

            }
        }

		private void albumPivot_LoadingPivotItem(object sender, PivotItemEventArgs e)
		{
			if (e.Item != null)
			{
				e.Item.Visibility = System.Windows.Visibility.Visible;

				var context = e.Item.DataContext as BaconographyPortable.ViewModel.LinkedPictureViewModel.LinkedPicture;

				if (context != null && _imageOrigins.ContainsKey(e.Item))
				{
					context.ImageSource = _imageOrigins[e.Item];
				}

				_currentItem = e.Item;
			}
		}

		private void albumPivot_UnloadingPivotItem(object sender, PivotItemEventArgs e)
		{
			if (e.Item != null)
			{
				e.Item.Visibility = System.Windows.Visibility.Collapsed;

				var context = e.Item.DataContext as BaconographyPortable.ViewModel.LinkedPictureViewModel.LinkedPicture;

				if (context != null)
				{	
					if (context.ImageSource is string)
					{
						if (!_imageOrigins.ContainsKey(e.Item))
						{
							_imageOrigins.Add(e.Item, context.ImageSource as String);
						}

						context.ImageSource = null;
					}
				}
			}
		}

        private void Caption_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (caption.TextWrapping == System.Windows.TextWrapping.Wrap)
            {
                caption.TextWrapping = System.Windows.TextWrapping.NoWrap;
                caption.TextTrimming = System.Windows.TextTrimming.WordEllipsis;
            }
            else
            {
                caption.TextWrapping = System.Windows.TextWrapping.Wrap;
                caption.TextTrimming = System.Windows.TextTrimming.None;
            }
        }

        private void Caption_ManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            if (caption.TextWrapping == System.Windows.TextWrapping.Wrap)
            {
                caption.TextWrapping = System.Windows.TextWrapping.NoWrap;
                caption.TextTrimming = System.Windows.TextTrimming.WordEllipsis;
            }
            else
            {
                caption.TextWrapping = System.Windows.TextWrapping.Wrap;
                caption.TextTrimming = System.Windows.TextTrimming.None;
            }
        }

        private Tuple<string, IEnumerable<Tuple<string, string>>, string> MakeSerializable(LinkedPictureViewModel vm)
        {
            return Tuple.Create(vm.LinkTitle, vm.Pictures.Select(linkedPicture => Tuple.Create(linkedPicture.Title, linkedPicture.Url)), vm.LinkId);
        }

        private async void myGridGestureListener_Flick(object sender, FlickGestureEventArgs e)
        {
            if (e.Direction == System.Windows.Controls.Orientation.Vertical)
            {
                //Up
                if (e.VerticalVelocity < -1500)
                {
                    var next = await _pictureViewModel.Next();
                    if (next != null)
                    {
                        TransitionService.SetNavigationOutTransition(this,
                            new NavigationOutTransition()
                            {
                                Forward = new SlideTransition()
                                {
                                    Mode = SlideTransitionMode.SlideUpFadeOut
                                }
                            }
                        );
                        ServiceLocator.Current.GetInstance<INavigationService>().Navigate(typeof(LinkedPictureView), MakeSerializable(next));
                    }
                   
                    
                }
                else if (e.VerticalVelocity > 1500) //Down
                {
                    var previous = await _pictureViewModel.Previous();
                    if (previous != null)
                    {
                        ServiceLocator.Current.GetInstance<INavigationService>().Navigate(typeof(LinkedPictureView), MakeSerializable(previous));
                    }
                }
            }
        }

    }
}
