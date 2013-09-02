using BaconographyPortable.Common;
using BaconographyPortable.Messages;
using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
using BaconographyWP8.Common;
using BaconographyWP8.Converters;
using BaconographyWP8.PlatformServices;
using BaconographyWP8Core;
using BaconographyWP8Core.Common;
using BaconographyWP8Core.View;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Controls;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Xna.Framework.Media;
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
            _saveCommand = new RelayCommand(SaveImage_Tap);
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

            if (e.NavigationMode == NavigationMode.New && e.IsNavigationInitiator)
            {
                
                var absPath = e.Uri.ToString().Contains('?') ? e.Uri.ToString().Substring(0, e.Uri.ToString().IndexOf("?")) : e.Uri.ToString();
                if (absPath == "/BaconographyWP8Core;component/View/LinkedPictureView.xaml")
                {
                    CleanupImageSource();
                    ServiceLocator.Current.GetInstance<INavigationService>().RemoveBackEntry();
                }
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

		private async void albumPivot_LoadingPivotItem(object sender, PivotItemEventArgs e)
		{
            if (e.Item != null)
            {
                var itemTpl = GenerateItemTripplet(e.Item);
                if (itemTpl.Item2 != null && itemTpl.Item2.Content == null)
                {
                    lock (itemTpl.Item2)
                    {
                        if(itemTpl.Item2.Content == null)
                            itemTpl.Item2.Content = ReifiedAlbumItemConverter.MapPictureVM(itemTpl.Item2.DataContext as ViewModelBase);
                    }
                }

                await Task.Yield();

                if (itemTpl.Item3 != null && itemTpl.Item3.Content == null && _priorItem != itemTpl.Item2)
                {
                    lock (itemTpl.Item3)
                    {
                        if (itemTpl.Item3.Content == null)
                            itemTpl.Item3.Content = ReifiedAlbumItemConverter.MapPictureVM(itemTpl.Item3.DataContext as ViewModelBase);
                    }
                }

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

        public void myGridGestureListener_Flick(object sender, FlickGestureEventArgs e)
        {
            FlipViewUtility.FlickHandler(sender, e, DataContext as ViewModelBase, this);
        }

        private RelayCommand _saveCommand;
        public RelayCommand SaveCommand
        {
            get
            {
                return _saveCommand;
            }
        }

        private async void SaveImage_Tap()
        {
            var linkedPicture = _pictureViewModel.Pictures.ToList()[albumPivot.SelectedIndex];
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });
            if (linkedPicture != null)
            {
                MediaLibrary library = new MediaLibrary();
                var libraryPicture = library.SavePicture(linkedPicture.Url.Substring(linkedPicture.Url.LastIndexOf('/') + 1), await ImagesService.ImageStreamFromUrl(linkedPicture.Url));
                var notificationService = ServiceLocator.Current.GetInstance<INotificationService>();
                if (libraryPicture != null)
                    notificationService.CreateNotification("Picture saved.");
                else
                    notificationService.CreateNotification("Error downloading picture.");
            }
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
        }

    }
}
