// ===============================================================================
// AnimatedImage.cs
// .NET Image Tools
// ===============================================================================
// Copyright (c) .NET Image Tools Development Group. 
// All rights reserved.
// ===============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ImageTools.Controls
{
    /// <summary>
    /// Represents a control, that displays an image or an animated image.
    /// </summary>
    /// <remarks>
    /// 	<para>
    ///         This control only define the 'Image' template parts.
    ///         This template part renders the image after it is converted to a writeable bitmap.
    ///     </para>
    /// 	<para>
    /// 	    This class has some breaking changes:
    ///         <list type="bullet">
    /// 			<item>
    /// 			    The image is able to render images that has not been loaded yet, but not able
    ///                 to render empty images that are filled after they are assinged. Assign the image again
    ///                 when you change the pixels by code.
    ///             </item>
    ///             <item>
    ///                 This control is only able to render extended images. This reduces the complexity
    ///                 of this class and is type safe. Use the image converter when you directly want to bind 
    ///                 string or uris.
    ///             </item>
    /// 	    </list>
    /// 	</para>
    /// </remarks>
    [TemplatePart(Name = AnimatedImage.ImagePart, Type = typeof(System.Windows.Controls.Image))]
    public class AnimatedImage : Control
    {
        #region Constants

        /// <summary>
        /// Defines the name of the 'Image' template part.
        /// This template part renders the image after it is converted to a writeable bitmap.
        /// </summary>
        public const string ImagePart = "Image";

        #endregion

        #region Invariant

#if !WINDOWS_PHONE
        [ContractInvariantMethod]
        private void AnimatedImageInvariantMethod()
        {
            Contract.Invariant(_animationFrameIndex >= 0);
            Contract.Invariant(_animationTimer != null);
            Contract.Invariant(_frames != null);
        }
#endif

        #endregion

        #region Fields

        private Image _image;
        private DispatcherTimer _animationTimer;
		private List<KeyValuePair<ImageBase, WeakReference>> _frames = new List<KeyValuePair<ImageBase, WeakReference>>();
		private List<ImageSource> _framerefs = new List<ImageSource>();
        private int _animationFrameIndex;
        private bool _isLoadingImage;

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the loading of the image has been completed.
        /// </summary>
        public event EventHandler LoadingCompleted;
        /// <summary>
        /// Raises the <see cref="E:LoadingCompleted"/> event.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected virtual void OnLoadingCompleted(EventArgs e)
        {
            EventHandler eventHandler = LoadingCompleted;

            if (eventHandler != null)
            {
                eventHandler(this, e);
            }
        }

        /// <summary>
        /// Occurs when the loading of the image failed.
        /// </summary>
        public event EventHandler<UnhandledExceptionEventArgs> LoadingFailed;
        /// <summary>
        /// Raises the <see cref="E:LoadingFailed"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.UnhandledExceptionEventArgs"/> instance 
        /// containing the event data.</param>
        protected virtual void OnLoadingFailed(UnhandledExceptionEventArgs e)
        {
            EventHandler<UnhandledExceptionEventArgs> eventHandler = LoadingFailed;

            if (eventHandler != null)
            {
                eventHandler(this, e);
            }
        }

        #endregion
    
        #region Dependency Properties

        /// <summary>
        /// Defines the <see cref="Pause"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PauseProperty =
            DependencyProperty.Register("Pause", typeof(bool), typeof(AnimatedImage), new PropertyMetadata(false));
        /// <summary>
        /// Gets or sets a value indicating if the animation is paused.
        /// </summary>
        /// <value>A value indicating if the animation is paused.</value>
        public bool Pause
        {
            [ContractVerification(false)]
            get { return (bool)GetValue(PauseProperty); }
            set { SetValue(PauseProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Filter"/> dependency property. 
        /// </summary>
        public static readonly DependencyProperty FilterProperty =
            DependencyProperty.Register("Filter", typeof(IImageFilter), typeof(AnimatedImage), new PropertyMetadata(OnFilterPropertyChanged));
        /// <summary>
        /// Gets or sets the filter that will be used before the image will be applied.
        /// </summary>
        /// <value>The filter.</value>
        public IImageFilter Filter
        {
            get { return (IImageFilter)GetValue(FilterProperty); }
            set { SetValue(FilterProperty, value); }
        }

        private static void OnFilterPropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var owner = o as AnimatedImage;
            if (owner != null)
            {
                owner.OnFilterPropertyChanged();
            }
        }

        private void OnFilterPropertyChanged()
        {
            if (Source != null && Source.IsFilled)
            {
                LoadImage(Source);
            }
        }

        /// <summary>
        /// Identifies the <see cref="Stretch"/> dependency property. 
        /// </summary>
        public static readonly DependencyProperty StretchProperty =
            DependencyProperty.Register("Stretch", typeof(Stretch),
                typeof(AnimatedImage), new PropertyMetadata(Stretch.Uniform));
        /// <summary>
        /// Gets or sets a value that describes how an <see cref="AnimatedImage"/> 
        /// should be stretched to fill the destination rectangle. This is a dependency property.
        /// </summary>
        /// <value>A value of the enumeration that specifies how the source image is applied if the 
        /// Height and Width of the Image are specified and are different than the source image's height and width.
        /// The default value is Uniform.</value>
        public Stretch Stretch
        {
            [ContractVerification(false)]
            get { return (Stretch)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="AutoSize"/> dependency property. 
        /// </summary>
        public static readonly DependencyProperty AutoSizeProperty =
            DependencyProperty.Register("AutoSize", typeof(bool),
                typeof(AnimatedImage), null);
        /// <summary>
        /// Gets or sets a value indicating whether the control should be auto sized. If the value is true
        /// the control will get the width and the height of its image source. This is a 
        /// dependency property.
        /// </summary>
        /// <value><c>true</c> if the size of the control should be set to the image
        /// width and height; otherwise, <c>false</c>.</value>
        public bool AutoSize
        {
            [ContractVerification(false)]
            get { return (bool)GetValue(AutoSizeProperty); }
            set { SetValue(AutoSizeProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="AnimationMode"/> dependency property. 
        /// </summary>
        public static readonly DependencyProperty AnimationModeProperty =
            DependencyProperty.Register("AnimationMode", typeof(AnimationMode),
                typeof(AnimatedImage), new PropertyMetadata(AnimationMode.Repeat));
        /// <summary>
        /// Gets or sets the animation mode of the image. This property will be just
        /// ignored if the specified source is not an animated image.
        /// </summary>
        /// <value>A value of the enumeration, that defines how to animate the image.</value>
        public AnimationMode AnimationMode
        {
            [ContractVerification(false)]
            get { return (AnimationMode)GetValue(AnimationModeProperty); }
            set { SetValue(AnimationModeProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Source"/> dependency property. 
        /// </summary>
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(ExtendedImage),
                typeof(AnimatedImage), new PropertyMetadata(new PropertyChangedCallback(OnSourcePropertyChanged)));
        /// <summary>
        /// Gets or sets the source for the image.
        /// </summary>
        /// <value>The source of the image control.</value>
        /// <remarks>
        /// The property supports the following types:
        /// <list type="table">
        /// <listheader>
        /// 	<term>Type</term>
        /// 	<description>Description</description>
        /// </listheader>
        /// <item>
        /// 	<term><see cref="String"/></term>
        /// 	<description>A string will be transformed to a <see cref="Uri"/> object with a relative path. A new BitmapImage
        ///     will be loaded asynchronously and assigned to the internal image element. Only png and .jpeg files
        ///     are supported usings string directly.</description>
        /// </item>
        /// <item>
        /// 	<term><see cref="ImageSource"/></term>
        /// 	<description>The image source will be directly assigned. No animations will be used.</description>
        /// </item>
        /// <item>
        /// 	<term><see cref="AnimatedImage"/></term>
        /// 	<description>The image will be assigned. Depending of the fact, if it is an animated image or not, 
        /// 	the animation will be started immediatly.</description>
        /// </item>
        /// 	</list>
        /// </remarks>
        /// <exception cref="ArgumentException">The specified value is not supported. Must be one of the types 
        /// defined below.</exception>
        public ExtendedImage Source
        {
            get { return (ExtendedImage)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        /// <summary>
        /// Called when source property is changed.
        /// </summary>
        /// <param name="d">The dependency object, which raised the event.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> 
        /// instance containing the event data.</param>
        private static void OnSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var owner = d as AnimatedImage;
            if (owner != null)
            {
                owner.OnSourceChanged();
            }
        }

        /// <summary>
        /// Called when the value of the source property is changed.
        /// </summary>
        protected virtual void OnSourceChanged()
        {
            if (_image != null && Source != null)
            {
                if (!Source.IsFilled || Source.IsLoading)
                {
                    Source.LoadingCompleted += new EventHandler(image_LoadingCompleted);
                    Source.LoadingFailed += new EventHandler<UnhandledExceptionEventArgs>(image_LoadingFailed);
                }
                else
                {
                    LoadImage(Source);
                }
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AnimatedImage"/> class.
        /// </summary>
        public AnimatedImage()
        {
            DefaultStyleKey = typeof(AnimatedImage);

            _animationTimer = new DispatcherTimer();
            _animationTimer.Tick += new EventHandler(timer_Tick);
        }

        #endregion

        #region Methods

		/// <summary>
		/// Utility method to ensure that all image and source references have been nulled out.
		/// </summary>
		public void Dispose()
		{
			if (_image != null)
			{
				_image.Source = null;
			}
			if (_framerefs != null)
			{
				_framerefs.Clear();
			}
			if (_frames != null)
			{
				_frames.Clear();
			}
			if (Source != null)
			{
				Source.UriSource = null;
				Source = null;
			}
			if (_animationTimer != null)
			{
				_animationTimer.Stop();
			}
		}

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or 
        /// internal processes (such as a rebuilding layout pass) 
        /// call <see cref="M:System.Windows.Controls.Control.ApplyTemplate"/>. 
        /// In simplest terms, this means the method is called just before a UI element 
        /// displays in an application. For more information, see Remarks.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            BindXaml();

            OnSourceChanged();
        }

        private void BindXaml()
        {
            _image = GetTemplateChild(ImagePart) as Image;
        }

        private void LoadImage(ExtendedImage image)
        {
            Contract.Requires(image != null);

            if (!_isLoadingImage)
            {
                _isLoadingImage = true;

                if (Filter != null)
                {
                    IImageFilter filter = Filter;

                    ThreadPool.QueueUserWorkItem(x =>
                        {
                            ExtendedImage filteredImage = ExtendedImage.ApplyFilters(image, filter);

                            Dispatcher.BeginInvoke(() => AssignImage(filteredImage));
                        });
                }
                else
                {
                    AssignImage(image);
                }
            }
        }

        private void AssignImage(ExtendedImage image)
        {
            Contract.Requires(image != null);

            _isLoadingImage = false;

            if (image.IsFilled)
            {
                _frames.Clear();

                WriteableBitmap imageBitmap = image.ToBitmap();

                if (imageBitmap == null)
                {
                    Stop();
                    return;
                }

                if (image.IsAnimated && AnimationMode != AnimationMode.None)
                {
                    Stop();

                    List<ImageBase> frames = new List<ImageBase>();
                    frames.Add(image);
                    frames.AddRange(image.Frames.OfType<ImageBase>());

                    foreach (ImageBase frame in frames)
                    {
                        if (frame != null && frame.IsFilled)
                        {
                            var bitmap = frame.ToBitmap();
                            if (bitmap == null)
                            {
                                Stop();
                                return;
                            }
                            _framerefs.Add(bitmap);
							_frames.Add(new KeyValuePair<ImageBase, WeakReference>(frame, new WeakReference(_framerefs[_framerefs.Count - 1])));
                        }
                    }

                    AnimateImage();

                    Start();
                }
                else
                {
                    if (_image != null)
                    {
                        _image.Source = imageBitmap;
                    }
                }
            }
        }

        private void image_LoadingCompleted(object sender, EventArgs e)
        {
            ExtendedImage image = sender as ExtendedImage;

            Dispatcher.BeginInvoke(() =>
                {
                    LoadImage(image);

                    image.LoadingCompleted -= new EventHandler(image_LoadingCompleted);
                    image.LoadingFailed -= new EventHandler<UnhandledExceptionEventArgs>(image_LoadingFailed);

                    OnLoadingCompleted(e);
                });
        }

        private void image_LoadingFailed(object sender, UnhandledExceptionEventArgs e)
        {
            ExtendedImage image = sender as ExtendedImage;

            Dispatcher.BeginInvoke(() =>
                {
                    image.LoadingCompleted -= new EventHandler(image_LoadingCompleted);
                    image.LoadingFailed -= new EventHandler<UnhandledExceptionEventArgs>(image_LoadingFailed);
                   
                    OnLoadingFailed(e);
                });
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (!Pause)
            {
                AnimateImage();
            }
        }

        private void AnimateImage()
        {
            if (_frames != null && _animationFrameIndex < _frames.Count)
            {
                var currentFrame = _frames[_animationFrameIndex];

                if (currentFrame.Key != null)
                {
                    if (_image != null)
                    {
                        _image.Source = currentFrame.Value.Target as ImageSource;
                    }

					int delay = currentFrame.Key.DelayTime * 10;
					if (currentFrame.Key.DelayTime < 5 && currentFrame.Key.DelayTime > 2)
					{
						delay = currentFrame.Key.DelayTime * 10 - 10;
					}
					_animationTimer.Interval = new TimeSpan(0, 0, 0, 0, delay);
                    _animationFrameIndex++;

                    if (_animationFrameIndex == _frames.Count)
                    {
                        if (AnimationMode == AnimationMode.PlayOnce)
                        {
                            Stop();
                        }

                        _animationFrameIndex = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Provides the behavior for the Measure pass of Silverlight layout. 
        /// Classes can override this method to define their own Measure pass behavior.
        /// </summary>
        /// <param name="availableSize">The available size that this object can give to child objects.</param>
        /// <returns>
        /// The size that this object determines it needs during layout, based on its 
        /// calculations of the allocated sizes for child objects; 
        /// or based on other considerations, such as a fixed container size.
        /// </returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (Source != null && Source.IsFilled && AutoSize)
            {
                return new Size(Source.PixelWidth, Source.PixelHeight);
            }
            else
            {
                return base.MeasureOverride(availableSize);
            }
        }

        /// <summary>
        /// Starts the animation. If there is no image assigned or the 
        /// assigned image is not a animated image, this method will just be ignored. If 
        /// the animation was paused, the animation will continue where it was stopped.
        /// </summary>
        public void Start()
        {
            Pause = false;

            _animationTimer.Start();
        }

        /// <summary>
        /// Stops the animation. If there is no image assigned or the 
        /// assigned image is not a animated image, this method will just be ignored.
        /// </summary>
        public void Stop()
        {
            _animationFrameIndex = 0;
			if (_animationTimer != null)
				_animationTimer.Stop();
        }

        #endregion
    }
}
