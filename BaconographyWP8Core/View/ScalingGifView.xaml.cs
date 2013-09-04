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
            Loaded += ScalingGifView_Loaded;
		}

        void ScalingGifView_Loaded(object sender, RoutedEventArgs e)
        {
            _loaded = true;
            if (_interop != null)
            {
                image.SetContentProvider(_interop.CreateContentProvider());

                var result = CoerceScaleImpl(viewport.ActualWidth, viewport.ActualHeight, _interop.Width, _interop.Height, 0.0);
                _scale = _coercedScale = _minScale = result.Item1;
                ResizeImage(true);
            }
        }

        const double MaxScale = 10;

		double _scale = 1.0;
		double _minScale;
		double _coercedScale;
		double _originalScale;

        bool _loaded = false;
		Size _viewportSize;
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

                viewport.Bounds = new Rect(0, 0, newWidth, newHeight);

                if (center)
                {
                    viewport.SetViewportOrigin(
                        new Point(
                            Math.Round(newWidth / 2),
                            Math.Round(newHeight / 2)
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
            if (viewport != null && _interop != null && _interop.Height != 0 && _interop.Width != 0)
            {
                var result = CoerceScaleImpl(viewport.ActualWidth, viewport.ActualHeight, _interop.Width, _interop.Height, 0.0);
                _minScale = result.Item1;
                _coercedScale = _scale = result.Item2;
            }

        }

        private static Tuple<double, double> CoerceScaleImpl(double viewWidth, double viewHeight, double bitmapWidth, double bitmapHeight, double scale)
        {
            double minX = viewWidth / bitmapWidth;
            double minY = viewHeight / bitmapHeight;
            var minScale = Math.Min(minX, minY);

            return Tuple.Create(minScale, Math.Min(MaxScale, Math.Max(scale, minScale)));

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
                    SetContentProvider(value as byte[]);
                }
				SetValue(ImageSourceProperty, value);
			}
		}

        private void SetContentProvider(byte[] asset)
        {
            try
            {
                _interop = new Direct3DInterop(asset);
                // Set native resolution in pixels
                _interop.WindowBounds = _interop.RenderResolution = _interop.NativeResolution = new Windows.Foundation.Size(_interop.Width, _interop.Height);
                image.Height = _interop.Height;
                image.Width = _interop.Width;
                // Hook-up native component to DrawingSurface
                if (_loaded)
                {
                    image.SetContentProvider(_interop.CreateContentProvider());

                    CoerceScale(true);
                    ResizeImage(true);
                }
            }
            catch
            {
                ServiceLocator.Current.GetInstance<INotificationService>().CreateNotification("Invalid Gif detected");
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
            Point center = e.GetPosition(image);
            _relativeMidpoint = new Point(center.X / image.ActualWidth, center.Y / image.ActualHeight);

            var xform = image.TransformToVisual(viewport);
            _screenMidpoint = xform.Transform(center);
            

            _coercedScale = _scale = _originalScale * e.DistanceRatio;
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
        }

        private void myGridGestureListener_PinchCompleted(object sender, PinchGestureEventArgs e)
        {
            _scale = _coercedScale;
        }
	}
}
