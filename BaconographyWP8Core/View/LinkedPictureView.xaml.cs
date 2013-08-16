using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
using BaconographyWP8.Common;
using BaconographyWP8.Converters;
using BaconographyWP8Core;
using BaconographyWP8Core.Common;
using GalaSoft.MvvmLight;
using Microsoft.Phone.Controls;
using Microsoft.Practices.ServiceLocation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
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
        IViewModelContextService _viewModelContextService;
        ISmartOfflineService _smartOfflineService;
        public LinkedPictureView()
        {
            using (ServiceLocator.Current.GetInstance<ISuspendableWorkQueue>().HighValueOperationToken)
            {
                this.InitializeComponent();
            }
            _viewModelContextService = ServiceLocator.Current.GetInstance<IViewModelContextService>();
            _smartOfflineService = ServiceLocator.Current.GetInstance<ISmartOfflineService>();
            
        }

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
            {
				DataContext = _pictureViewModel;
            }

            
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

        Task _cleanup;
        private void CleanupImageSource()
        {
            try
            {
                ReifiedAlbumItemConverter.CancelSource.Cancel();
                ReifiedAlbumItemConverter.CancelSource = new CancellationTokenSource();
                this.State.Clear();
                foreach (var item in albumPivot.Items)
                {
                    if (item is PivotItem)
                    {

                        var content = ((PivotItem)item).Content;
                        if (content is ScalingGifView)
                        {
                            ((ScalingGifView)content).ImageSource = null;
                        }
                        else if (content is ScalingPictureView)
                        {
                            ((ScalingPictureView)content).ImageSource = null;
                        }

                        ((PivotItem)item).Content = null;

                        var context = ((PivotItem)item).DataContext as BaconographyPortable.ViewModel.LinkedPictureViewModel.LinkedPicture;
                        context.ImageSource = null;
                    }
                    else if (item is BaconographyPortable.ViewModel.LinkedPictureViewModel.LinkedPicture)
                    {
                        ((BaconographyPortable.ViewModel.LinkedPictureViewModel.LinkedPicture)item).ImageSource = null;
                    }
                }
                ((LinkedPictureViewModel)DataContext).Cleanup();
                if (albumPivot.ItemsSource is ObservableCollection<PivotItem>)
                {
                    ((ObservableCollection<PivotItem>)albumPivot.ItemsSource).Clear();
                }
                this.Content = null;
                if (_gcCount <= 0)
                    Task.Factory.StartNew(RunGC, TaskCreationOptions.LongRunning);
                
            }
            catch
            {

            }
        }

        private static int _gcCount = 0;
        private static void RunGC()
        {
            if (_gcCount >= 1)
                return;

            _gcCount++;
            for(int i = 0; i < 3; i++)
            {
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                GC.WaitForPendingFinalizers();
            }
            _gcCount--;
        }

        private PivotItem _priorItem;
        private PivotItem _currentItem;
        private PivotItem _nextItem;

        private Tuple<PivotItem, PivotItem, PivotItem> GenerateItemTripplet(PivotItem newCurrent)
        {
            PivotItem prior = null, current = newCurrent, next = null;
            var currentIndex = albumPivot.Items.IndexOf(newCurrent);
            if (currentIndex > 0)
                prior = albumPivot.Items[currentIndex - 1] as PivotItem;
            if (currentIndex + 1 < albumPivot.Items.Count)
                next = albumPivot.Items[currentIndex + 1] as PivotItem;

            return Tuple.Create(prior, current, next);
        }

		private void albumPivot_LoadingPivotItem(object sender, PivotItemEventArgs e)
		{
            if (e.Item != null)
            {
                var itemTpl = GenerateItemTripplet(e.Item);
                if(itemTpl.Item2 != null && itemTpl.Item2.Content == null)
                    itemTpl.Item2.Content = ReifiedAlbumItemConverter.MapPictureVM(itemTpl.Item2.DataContext as ViewModelBase);
                if (itemTpl.Item3 != null && itemTpl.Item3.Content == null)
                    itemTpl.Item3.Content = ReifiedAlbumItemConverter.MapPictureVM(itemTpl.Item3.DataContext as ViewModelBase);

                lock (this)
                {
                    _priorItem = itemTpl.Item1;
                    _currentItem = itemTpl.Item2;
                    _nextItem = itemTpl.Item3;
                }
            }
			
            
		}

		private void albumPivot_UnloadingPivotItem(object sender, PivotItemEventArgs e)
		{
            if (e.Item != null)
			{
                ClearItem(e.Item);
                if(_gcCount <= 0)
                    Task.Factory.StartNew(RunGC, TaskCreationOptions.LongRunning);
			}
		}

        private static void ClearItem(PivotItem item)
        {
            if (item.Content is ScalingGifView)
            {
                ((ScalingGifView)item.Content).ImageSource = null;
            }
            else if (item.Content is ScalingPictureView)
            {
                ((ScalingPictureView)item.Content).ImageSource = null;
            }
            item.Content = null;
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


        bool _flicking;
        private async void myGridGestureListener_Flick(object sender, FlickGestureEventArgs e)
        {
            if (_flicking)
                return;

            if (e.Direction == System.Windows.Controls.Orientation.Vertical)
            {
                //Up
                if (e.VerticalVelocity < -1500)
                {
                    _flicking = true;
                    try
                    {
                        using (ServiceLocator.Current.GetInstance<ISuspendableWorkQueue>().HighValueOperationToken)
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
                    }
                    finally
                    {
                        _flicking = false;
                    }
                   
                    
                }
                else if (e.VerticalVelocity > 1500) //Down
                {
                    _flicking = true;
                    try
                    {
                        using (ServiceLocator.Current.GetInstance<ISuspendableWorkQueue>().HighValueOperationToken)
                        {
                            var previous = await _pictureViewModel.Previous();
                            if (previous != null)
                            {
                                ServiceLocator.Current.GetInstance<INavigationService>().Navigate(typeof(LinkedPictureView), MakeSerializable(previous));
                            }
                        }
                    }
                    finally
                    {
                        _flicking = false;
                    }
                }
            }
        }

    }
}
