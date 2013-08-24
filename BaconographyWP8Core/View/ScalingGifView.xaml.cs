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
        private bool _initialLoad = true;

		/// <summary>
		/// Either the user has manipulated the image or the size of the viewport has changed. We only
		/// care about the size.
		/// </summary>
		void viewport_ViewportChanged(object sender, System.Windows.Controls.Primitives.ViewportChangedEventArgs e)
		{
			Size newSize = new Size(viewport.Viewport.Width, viewport.Viewport.Height);
			if (newSize != _viewportSize)
			{
				_viewportSize = newSize;
                if (!_initialLoad)
                {
                    CoerceScale(true);
                    ResizeImage(false);
                }
                else
                    _initialLoad = false;
			}
		}

		/// <summary>
		/// Adjust the size of the image according to the coerced scale factor. Optionally
		/// center the image, otherwise, try to keep the original midpoint of the pinch
		/// in the same spot on the screen regardless of the scale.
		/// </summary>
		/// <param name="center"></param>
		void ResizeImage(bool center)
		{
			if (_coercedScale != 0)
			{
				double newWidth;
				double newHeight;
				if (_interop != null)
				{
                    newWidth = image.Width = Math.Round(_interop.Width * _coercedScale);
                    newHeight = image.Height = Math.Round(_interop.Height * _coercedScale);
				}
				else return;

				//xform.ScaleX = xform.ScaleY = _coercedScale;

				viewport.Bounds = new Rect(0, 0, newWidth, newHeight);

				if (center)
				{
					viewport.SetViewportOrigin(
						new Point(
							Math.Round((newWidth - viewport.ActualWidth) / 2),
							Math.Round((newHeight - viewport.ActualHeight) / 2)
							));
				}
				else
				{
					Point newImgMid = new Point(newWidth * _relativeMidpoint.X, newHeight * _relativeMidpoint.Y);
					Point origin = new Point(newImgMid.X - _screenMidpoint.X, newImgMid.Y - _screenMidpoint.Y);
					viewport.SetViewportOrigin(origin);
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
			if (recompute && viewport != null)
			{
                _scale = 0.0;
				// Calculate the minimum scale to fit the viewport
				if (_interop != null)
				{
                    double minX = viewport.ActualWidth / _interop.Width;
                    double minY = viewport.ActualHeight / _interop.Height;
					_minScale = Math.Min(minX, minY);
					if (_minScale <= 0.0)
						_minScale = 1.0;
				}		
			}

			_coercedScale = Math.Min(MaxScale, Math.Max(_scale, _minScale));

		}


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
                else if (image != null && _interop == null && value is byte[])
                {
                    try
                    {
                        _interop = new Direct3DInterop(value as byte[]);

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
                    catch
                    {
                        ServiceLocator.Current.GetInstance<INotificationService>().CreateNotification("Invalid Gif detected");
                    }
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
            catch
            {
            }
            finally
            {
                Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
                Monitor.Exit(this);
            }
        }

        private void myGridGestureListener_PinchDelta(object sender, PinchGestureEventArgs e)
        {
            
            _pinching = true;
            Point center = e.GetPosition(image);
            _relativeMidpoint = new Point(center.X / image.ActualWidth, center.Y / image.ActualHeight);

            var xform = image.TransformToVisual(viewport);
            _screenMidpoint = xform.Transform(center);
            

            _scale = _originalScale * e.DistanceRatio;

            CoerceScale(false);
            ResizeImage(false);
        }

        private void myGridGestureListener_DoubleTap(object sender, Microsoft.Phone.Controls.GestureEventArgs e)
        {
            var point = e.GetPosition(image);
            _relativeMidpoint = new Point(point.X / image.ActualWidth, point.Y / image.ActualHeight);

            var xform = image.TransformToVisual(viewport);
            _screenMidpoint = xform.Transform(point);

            if (_coercedScale >= (_minScale * 2.5) || _coercedScale < 0)
                _coercedScale = _minScale;
            else
                _coercedScale *= 1.75;

            ResizeImage(false);
        }

        private void myGridGestureListener_PinchStarted(object sender, PinchStartedGestureEventArgs e)
        {
            _originalScale = _scale;
            _pinching = true;
        }

        private void myGridGestureListener_PinchCompleted(object sender, PinchGestureEventArgs e)
        {
            _scale = _coercedScale;
            _pinching = false;
        }
	}
}
