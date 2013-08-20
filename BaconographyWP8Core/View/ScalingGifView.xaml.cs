using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.ComponentModel;

using System.Windows.Media;
using BaconographyWP8.PlatformServices;
using System.Threading.Tasks;
using System.IO;
using BaconographyPortable.ViewModel;
using GalaSoft.MvvmLight;
using System;
using Microsoft.Practices.ServiceLocation;
using BaconographyPortable.Services;
using GalaSoft.MvvmLight.Messaging;
using BaconographyPortable.Messages;
using BaconographyWP8Core.Common;
using DXGifRenderWP8;
using System.Threading;

namespace BaconographyWP8.View
{
	public partial class ScalingGifView : UserControl
	{
		public ScalingGifView()
		{
			InitializeComponent();
		}

		const double MaxScale = 10;

		double _scale = 1.0;
		double _minScale;
		double _coercedScale;
		double _originalScale;

		Size _viewportSize;
		bool _pinching;
		Point _screenMidpoint;
		Point _relativeMidpoint;
        Direct3DInterop _interop;

		public static readonly DependencyProperty ImageSourceProperty =
			DependencyProperty.Register(
				"ImageSource",
				typeof(object),
				typeof(ScalingGifView),
				new PropertyMetadata(null)
			);

		public object ImageSource
		{
			get { return GetValue(ImageSourceProperty); }
			set
			{
				if (value == null && image != null)
				{
					image.SetContentProvider(null);
                    _interop = null;
				}
                else if (image != null && _interop == null && value is string)
				{
                    SetContentProvider(value as string);
					
				}
				SetValue(ImageSourceProperty, value);
			}
		}

        private async void SetContentProvider(string sourceUrl)
        {
            Monitor.Enter(this);
            try
            {
                if (_interop != null)
                    return;

                Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });
                var asset = await SimpleHttpService.GetBytes(sourceUrl);
                if (asset == null)
                    return;

                _interop = new Direct3DInterop(asset);

                // Set native resolution in pixels
                _interop.RenderResolution = _interop.NativeResolution = _interop.WindowBounds = new Windows.Foundation.Size(_interop.Width, _interop.Height);
                image.Height = _interop.Height;
                image.Width = _interop.Width;
                // Hook-up native component to DrawingSurface
                image.SetContentProvider(_interop.CreateContentProvider());
                _scale = 0;
                CoerceScale(true);
                _scale = _coercedScale;
                ResizeImage(true);
            }
            finally
            {
                Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
                Monitor.Exit(this);
            }
        }

		private static void OnSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var image = (ScalingGifView)d;
			image.ImageSource = e.NewValue;
		}


		/// <summary>
		/// Either the user has manipulated the image or the size of the viewport has changed. We only
		/// care about the size.
		/// </summary>
        //void viewport_ViewportChanged(object sender, System.Windows.Controls.Primitives.ViewportChangedEventArgs e)
        //{
			

        //    Size newSize = new Size(viewport.Viewport.Width, viewport.Viewport.Height);
        //    if (newSize != _viewportSize)
        //    {
        //        _viewportSize = newSize;
        //        CoerceScale(true);
        //        ResizeImage(false);
        //    }
        //}

		/// <summary>
		/// Handler for the ManipulationStarted event. Set initial state in case
		/// it becomes a pinch later.
		/// </summary>
        //void OnManipulationStarted(object sender, ManipulationStartedEventArgs e)
        //{
        //    _pinching = false;
        //    _originalScale = _scale;
        //}

		/// <summary>
		/// Handler for the ManipulationDelta event. It may or may not be a pinch. If it is not a 
		/// pinch, the ViewportControl will take care of it.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void OnManipulationDelta(object sender, ManipulationDeltaEventArgs e)
		{
            if (e.PinchManipulation != null)
            {
                var transform = (CompositeTransform)image.RenderTransform;

                // Scale Manipulation
                transform.ScaleX *= e.PinchManipulation.DeltaScale;
                transform.ScaleY *= e.PinchManipulation.DeltaScale;

                // Translate manipulation
                var originalCenter = e.PinchManipulation.Original.Center;
                var newCenter = e.PinchManipulation.Current.Center;
                transform.TranslateX =+ newCenter.X - originalCenter.X;
                transform.TranslateY =+ newCenter.Y - originalCenter.Y;


                // end 
                e.Handled = true;
            }
		}

		/// <summary>
		/// The manipulation has completed (no touch points anymore) so reset state.
		/// </summary>
        //void OnManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        //{
        //    _pinching = false;
        //    _scale = _coercedScale;
        //}


		/// <summary>
		/// Adjust the size of the image according to the coerced scale factor. Optionally
		/// center the image, otherwise, try to keep the original midpoint of the pinch
		/// in the same spot on the screen regardless of the scale.
		/// </summary>
		/// <param name="center"></param>
		void ResizeImage(bool center)
		{
            if (_coercedScale != 0 && image != null && _interop != null)
            {
                double newWidth;
                double newHeight;
                newWidth = Width = Math.Round(_interop.Width * _coercedScale);
                newHeight = Height = Math.Round(_interop.Width * _coercedScale);

                var transform = (CompositeTransform)image.RenderTransform;
                transform.ScaleX = transform.ScaleY = _coercedScale;

                if (center)
                {
                    transform.CenterX = Math.Round((newWidth - ActualWidth) / 2);
                    transform.CenterY = Math.Round((newHeight - ActualWidth) / 2);
                }
                else
                {
                    Point newImgMid = new Point(newWidth * _relativeMidpoint.X, newHeight * _relativeMidpoint.Y);
                    transform.CenterX = newImgMid.X - _screenMidpoint.X;
                    transform.CenterY = newImgMid.Y - _screenMidpoint.Y;
                }
            }
		}

		/// <summary>
		/// Coerce the scale into being within the proper range. Optionally compute the constraints 
		/// on the scale so that it will always fill the entire screen and will never get too big 
		/// to be contained in a hardware surface.
		/// </summary>
		/// <param name="recompute">Will recompute the min max scale if true.</param>
		void CoerceScale(bool recompute)
		{
            if (recompute && image != null && _interop != null)
            {
                // Calculate the minimum scale to fit the viewport
                double minX = ActualWidth / _interop.Width;
                double minY = ActualHeight / _interop.Height;
                _minScale = Math.Min(minX, minY);
                if (_minScale == 0.0)
                    _minScale = 1.0;
            }

            _coercedScale = Math.Min(MaxScale, Math.Max(_scale, _minScale));

		}

		/// <summary>
		/// When a new image is opened, set its initial scale.
		/// </summary>
		private void OnImageOpened(object sender, EventArgs e)
		{
			// Set scale to the minimum, and then save it.
			_scale = 0;
			CoerceScale(true);
			_scale = _coercedScale;
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
			ResizeImage(true);
		}

		/// <summary>
		/// When an animated image is opened and the load fails, kick out to browser
		/// </summary>
		private void OnLoadingFailed(object sender, EventArgs e)
		{
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
			var _navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
			var pvm = (LinkedPictureViewModel.LinkedPicture)DataContext;
			if (pvm.ImageSource is string)
			{
				_navigationService.GoBack();
				_navigationService.NavigateToExternalUri(new Uri(pvm.ImageSource as string));
			}
		}
	}
}
