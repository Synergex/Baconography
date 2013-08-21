using System;
using System.Collections.Generic;
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
using GalaSoft.MvvmLight.Messaging;
using BaconographyPortable.Messages;
using Microsoft.Practices.ServiceLocation;
using BaconographyPortable.Services;

namespace BaconographyWP8.View
{
	public partial class ScalingPictureView : UserControl
	{
		const double MaxScale = 10;

		double _scale = 1.0;
		double _minScale;
		double _coercedScale;
		double _originalScale;

		Size _viewportSize;
		bool _pinching;
		Point _screenMidpoint;
		Point _relativeMidpoint;

		BitmapImage _bitmap;

		public static readonly DependencyProperty ImageSourceProperty =
			DependencyProperty.Register(
				"ImageSource",
				typeof(object),
				typeof(ScalingPictureView),
				new PropertyMetadata(null, OnImageSourceChanged)
			);

        private static void OnImageSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var image = (ScalingPictureView)d;
            image.ImageSource = e.NewValue;
        }

        object _imageSource;
		public object ImageSource
		{
			get { return GetValue(ImageSourceProperty); }
			set
			{
                if (_imageSource != value)
                {
                    if (value == null)
                    {
                        if (_bitmap != null)
                        {
                            _bitmap.ImageOpened -= OnImageOpened;
                            _bitmap.ImageFailed -= _bitmap_ImageFailed;
                            _bitmap.UriSource = null;
                        }
                        _bitmap = null;
                        image.Source = null;
                    }
                    else if (value is string)
                    {
                        _bitmap = new BitmapImage();
                        _bitmap.CreateOptions = BitmapCreateOptions.None;
                        _bitmap.ImageOpened += OnImageOpened;
                        _bitmap.ImageFailed += _bitmap_ImageFailed;
                        Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });
                        _bitmap.UriSource = new Uri(value as string);
                    }
                    _imageSource = value;
                    SetValue(ImageSourceProperty, value);
                }
			}
		}

        void _bitmap_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
            ServiceLocator.Current.GetInstance<INotificationService>().CreateNotification("image failed to load: " + e.ErrorException);
        }

		/// <summary>
		/// This is a very simple page. We simply bind to the CurrentPicture property on the AlbumsViewModel
		/// </summary>
		public ScalingPictureView()
		{
			InitializeComponent();
		}

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
		/// Handler for the ManipulationStarted event. Set initial state in case
		/// it becomes a pinch later.
		/// </summary>
		void OnManipulationStarted(object sender, ManipulationStartedEventArgs e)
		{
			_pinching = false;
			_originalScale = _scale;
		}

		/// <summary>
		/// Handler for the ManipulationDelta event. It may or may not be a pinch. If it is not a 
		/// pinch, the ViewportControl will take care of it.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void OnManipulationDelta(object sender, ManipulationDeltaEventArgs e)
		{
			if (e.PinchManipulation != null && image != null)
			{
				e.Handled = true;

				if (!_pinching)
				{
					_pinching = true;
					Point center = e.PinchManipulation.Original.Center;
					_relativeMidpoint = new Point(center.X / image.ActualWidth, center.Y / image.ActualHeight);

					var xform = image.TransformToVisual(viewport);
					_screenMidpoint = xform.Transform(center);
				}

				_scale = _originalScale * e.PinchManipulation.CumulativeScale;

				CoerceScale(false);
				ResizeImage(false);
			}
			else if (_pinching)
			{
				_pinching = false;
				_originalScale = _scale = _coercedScale;
			}
		}

		/// <summary>
		/// The manipulation has completed (no touch points anymore) so reset state.
		/// </summary>
		void OnManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
		{
			_pinching = false;
			_scale = _coercedScale;
		}


		/// <summary>
		/// When a new image is opened, set its initial scale.
		/// </summary>
		void OnImageOpened(object sender, RoutedEventArgs e)
		{
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
            
			// Set scale to the minimum, and then save it.
			_scale = 0;
			CoerceScale(true);
			_scale = _coercedScale;

			ResizeImage(true);
            image.Source = _bitmap;
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
				if (_bitmap != null)
				{
					newWidth = canvas.Width = Math.Round(_bitmap.PixelWidth * _coercedScale);
					newHeight = canvas.Height = Math.Round(_bitmap.PixelHeight * _coercedScale);
				}
				else return;

				xform.ScaleX = xform.ScaleY = _coercedScale;

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
				if (_bitmap != null)
				{
					double minX = viewport.ActualWidth / _bitmap.PixelWidth;
					double minY = viewport.ActualHeight / _bitmap.PixelHeight;
					_minScale = Math.Min(minX, minY);
					if (_minScale <= 0.0)
						_minScale = 1.0;
				}		
			}

			_coercedScale = Math.Min(MaxScale, Math.Max(_scale, _minScale));

		}

        private void OnDoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
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
	}
}
