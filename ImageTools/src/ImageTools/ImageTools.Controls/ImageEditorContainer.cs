// ===============================================================================
// ImageEditorContainer.cs
// .NET Image Tools
// ===============================================================================
// Copyright (c) .NET Image Tools Development Group. 
// All rights reserved.
// ===============================================================================

using System;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ImageTools.Helpers;

namespace ImageTools.Controls
{
    /// <summary>
    /// Defines a container for editing images, where images can be zoomed or 
    /// or selected with a selection border.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This control defines the following template parts:
    ///         <list type="bullet"> 
    /// 		    <item>
    /// 			    <term>ImageElement</term>
    /// 			    <description>The animated image control, which renders the image that is edited.</description>
    /// 		    </item>
    /// 		    <item>
    /// 			    <term>ScrollViewer</term>
    /// 			    <description>Container of the image. Allows scrolling.</description>
    /// 		    </item>
    /// 		    <item>
    /// 			    <term>SelectionArea</term>
    /// 			    <description>The area which is used to subscribe to mouse events to calculate 
    /// 			    the selection border.</description>
    /// 		    </item>
    /// 		    <item>
    /// 			    <term>SelectionBorder</term>
    /// 			    <description>A border to select an area of the image. It should be
    /// 			    should be filled with a transparent or semi-transparent color.</description>
    /// 		    </item>
    /// 	    </list>
    ///     </para>
    /// </remarks>
    [TemplatePart(Name = ImageEditorContainer.ScrollViewerElementPart, Type = typeof(ScrollViewer))]
    [TemplatePart(Name = ImageEditorContainer.SelectionAreaPart, Type = typeof(Control))]
    [TemplatePart(Name = ImageEditorContainer.SelectionBorderPart, Type = typeof(Border))]
    [TemplatePart(Name = ImageEditorContainer.ImagePart, Type = typeof(AnimatedImage))]
    public class ImageEditorContainer : Control
    {
        #region Constants

        /// <summary>
        /// Defines the name for the 'ScrollViewer' template part, which is used 
        /// to scroll the image when it is zoomed in.
        /// </summary>
        public const string ScrollViewerElementPart = "ScrollViewer";
        /// <summary>
        /// Defines the name for the selection area template part.
        /// </summary>
        public const string SelectionAreaPart = "SelectionArea";
        /// <summary>
        /// Defines the name for the selection border template part.
        /// </summary>
        public const string SelectionBorderPart = "SelectionBorder";
        /// <summary>
        /// Defines the name for the image element template part.
        /// </summary>
        public const string ImagePart = "Image";

        #endregion

        #region Fields

        private ScrollViewer _scrollViewer;
        private FrameworkElement _selectionArea;
        private Border _selectionBorder;
        private double _scaleFactor;
        private AnimatedImage _imageElement;
        private bool _isMoving;
        private bool _isSelecting;
        private Point _lastMousePosition;
        private Rect _mouseSelection;

        #endregion

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="RequiredSelectionHeight"/> property.
        /// </summary>
        public static readonly DependencyProperty RequiredSelectionHeightProperty =
            DependencyProperty.Register("RequiredSelectionHeight", typeof(int), typeof(ImageEditorContainer), null);
        /// <summary>
        /// Gets or sets the height of the selection border.
        /// </summary>
        /// <value>The height of the selection.</value>
        public int RequiredSelectionHeight
        {
            [ContractVerification(false)]
            get { return (int)GetValue(RequiredSelectionHeightProperty); }
            set { SetValue(RequiredSelectionHeightProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="RequiredSelectionWidth"/> property.
        /// </summary>
        public static readonly DependencyProperty RequiredSelectionWidthProperty =
            DependencyProperty.Register("RequiredSelectionWidth", typeof(int), typeof(ImageEditorContainer), null);
        /// <summary>
        /// Gets or sets the width of the selection border.
        /// </summary>
        /// <value>The width of the selection.</value>
        public int RequiredSelectionWidth
        {
            [ContractVerification(false)]
            get { return (int)GetValue(RequiredSelectionWidthProperty); }
            set { SetValue(RequiredSelectionWidthProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="SelectionMode"/> property.
        /// </summary>
        public static readonly DependencyProperty SelectionModeProperty =
            DependencyProperty.Register("SelectionMode", typeof(ImageEditorSelectionMode), typeof(ImageEditorContainer), null);
        /// <summary>
        /// Gets or sets the selection mode.
        /// </summary>
        /// <value>The selection mode.</value>
        public ImageEditorSelectionMode SelectionMode
        {
            [ContractVerification(false)]
            get { return (ImageEditorSelectionMode)GetValue(SelectionModeProperty); }
            set { SetValue(SelectionModeProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Selection"/> property.
        /// </summary>
        public static readonly DependencyProperty SelectionProperty =
            DependencyProperty.Register("Selection", typeof(Rect), typeof(ImageEditorContainer), new PropertyMetadata(OnSelectionChanged));
        /// <summary>
        /// Gets or sets the selection.
        /// </summary>
        /// <value>The selection.</value>
        public Rect Selection
        {
            [ContractVerification(false)]
            get { return (Rect)GetValue(SelectionProperty); }
            set { SetValue(SelectionProperty, value); }
        }

        private static void OnSelectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var owner = d as ImageEditorContainer;
            if (owner != null)
            {
                owner.OnSelectionChanged();
            }
        }

        private void OnSelectionChanged()
        {
            if (Source != null && Source.IsFilled)
            {
                Guard.GreaterEquals(Selection.X, 0, "Selection", "Selection.X must be greater or equals than zero.");
                Guard.GreaterEquals(Selection.Y, 0, "Selection", "Selection.Y must be greater or equals than zero.");

                Guard.LessEquals(Selection.Right,  Source.PixelWidth,  "Selection", "Selection.Right must be less or equals than pixel width.");
                Guard.LessEquals(Selection.Bottom, Source.PixelHeight, "Selection", "Selection.Bottom must be greater or equals than pixel height.");
            }

            UpdateSelectionBorder();
        }

        /// <summary>
        /// Identifies the <see cref="Source"/> dependency property. 
        /// </summary>
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(ExtendedImage), typeof(ImageEditorContainer), new PropertyMetadata(OnSourceChanged));
        /// <summary>
        /// Gets or sets the source for the image.
        /// </summary>
        public ExtendedImage Source
        {
            [ContractVerification(false)]
            get { return (ExtendedImage)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var owner = d as ImageEditorContainer;
            if (owner != null)
            {
                owner.OnSourceChanged();
            }
        }

        private void OnSourceChanged()
        {
            Selection = Extensions.ZeroRect;

            if (Source != null)
            {
                if (!Source.IsFilled)
                {
                    Source.LoadingCompleted += new EventHandler(Source_LoadingCompleted);
                }
                else
                {
                    Selection = Extensions.ZeroRect;

                    ScaleImage();
                }
            }
        }

        private void Source_LoadingCompleted(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(() => OnSourceChanged());
        }

        /// <summary>
        /// Defines the <see cref="ScalingMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ScalingModeProperty =
            DependencyProperty.Register("ScalingMode", typeof(ImageEditorScalingMode), typeof(ImageEditorContainer), new PropertyMetadata(ImageEditorScalingMode.Auto, new PropertyChangedCallback(OnScalingModeChanged)));
        /// <summary>
        /// Gets or sets the selection mode.
        /// </summary>
        /// <value>The selection mode.</value>
        public ImageEditorScalingMode ScalingMode
        {
            [ContractVerification(false)]
            get { return (ImageEditorScalingMode)GetValue(ScalingModeProperty); }
            set { SetValue(ScalingModeProperty, value); }
        }

        private static void OnScalingModeChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var owner = o as ImageEditorContainer;
            if (owner != null)
            {
                owner.ScaleImage();
            }
        }

        /// <summary>
        /// Identifies the <see cref="Scaling"/> property.
        /// </summary>
        public static readonly DependencyProperty ScalingProperty =
            DependencyProperty.Register("Scaling", typeof(double), typeof(ImageEditorContainer), new PropertyMetadata(OnScalingChanged));
        /// <summary>
        /// Gets or sets the scaling, which defines how the image should be scaled.
        /// </summary>
        /// <value>The scaling.</value>
        /// <remarks>Define the zomming factor with a positive value or apply a negative value, if the image should
        /// be scaled to fit to the screeen.</remarks>
        public double Scaling
        {
            [ContractVerification(false)]
            get { return (double)GetValue(ScalingProperty); }
            set { SetValue(ScalingProperty, value); }
        }

        private static void OnScalingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var owner = d as ImageEditorContainer;
            if (owner != null)
            {
                owner.OnScalingChanged();
            }
        }

        private void OnScalingChanged()
        {
            Guard.GreaterThan(Scaling, 0, "Scaling", "Scaling cannot be less or equals than zero.");

            ScaleImage();
        }

        /// <summary>
        /// Identifies the <see cref="MoveSelection"/> property.
        /// </summary>
        public static readonly DependencyProperty MoveSelectionProperty =
            DependencyProperty.Register("MoveSelection", typeof(bool), typeof(ImageEditorContainer), null);
        /// <summary>
        /// Gets or sets the interaction mode, which defines how the control behaves when the mouse is moved.
        /// </summary>
        /// <value>The interaction mode.</value>
        public bool MoveSelection
        {
            [ContractVerification(false)]
            get { return (bool)GetValue(MoveSelectionProperty); }
            set { SetValue(MoveSelectionProperty, value); }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageEditorContainer"/> class.
        /// </summary>
        public ImageEditorContainer()
        {
            DefaultStyleKey = typeof(ImageEditorContainer);

            SizeChanged += new SizeChangedEventHandler(ImageEditorContainer_SizeChanged);
        }

        #endregion

        #region Methods

        #region Template Handling

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or 
        /// internal processes (such as a rebuilding layout pass) 
        /// call <see cref="M:System.Windows.Controls.Control.ApplyTemplate"/>.
        /// </summary>
        public override void OnApplyTemplate()
        {
            BindXamlElements();

            if (!DesignerProperties.IsInDesignTool)
            {
                ScaleImage();
            }

            base.OnApplyTemplate();
        }

        private void BindXamlElements()
        {
            _imageElement = GetTemplateChild(ImagePart) as AnimatedImage;

            _selectionArea = GetTemplateChild(SelectionAreaPart) as FrameworkElement;

            if (_selectionArea != null)
            {
                _selectionArea.MouseLeftButtonDown += new MouseButtonEventHandler(selectionArea_MouseLeftButtonDown);
                _selectionArea.LostMouseCapture += new MouseEventHandler(selectionArea_LostMouseCapture);
                _selectionArea.MouseMove += new MouseEventHandler(selectionArea_MouseMove);
            }

            _selectionBorder = GetTemplateChild(SelectionBorderPart) as Border;

            if (_selectionBorder != null)
            {
                _selectionBorder.MouseLeftButtonDown += new MouseButtonEventHandler(selectionBorder_MouseLeftButtonDown);
                _selectionBorder.LostMouseCapture += new MouseEventHandler(selectionBorder_LostMouseCapture);
                _selectionBorder.MouseMove += new MouseEventHandler(selectionBorder_MouseMove);
            }

            _scrollViewer = GetTemplateChild(ScrollViewerElementPart) as ScrollViewer;
        }

        #endregion

        private void ImageEditorContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ScaleImage();
        }

        private void ScaleImage()
        {
            if (_scrollViewer != null && _scrollViewer.ViewportHeight > 0 && _imageElement != null)
            {
                if (ScalingMode == Controls.ImageEditorScalingMode.Auto)
                {
                    if (Source != null && Source.IsFilled)
                    {
                        double newWidth = 0;

                        double viewportRatio = _scrollViewer.ViewportWidth / _scrollViewer.ViewportHeight;

                        if (viewportRatio > Source.PixelRatio)
                        {
                            newWidth = Math.Max(0, _scrollViewer.ViewportHeight - 20) * Source.PixelRatio;
                        }
                        else
                        {
                            newWidth = Math.Max(0, _scrollViewer.ViewportWidth - 20);
                        }

                        Scaling = newWidth / Source.PixelWidth;
                    }
                }

                if (Scaling > 0)
                {
                    double oldScaling = _scaleFactor;

                    _scaleFactor = Scaling;

                    if (Source != null)
                    {
                        _imageElement.Width  = Source.PixelWidth * Scaling;
                        _imageElement.Height = Source.PixelHeight * Scaling;
                    }

                    if (_scaleFactor > 0 && _scaleFactor.IsNumber())
                    {
                        double dScale = oldScaling == 0 || !oldScaling.IsNumber() ?
                                        _scaleFactor :
                                        _scaleFactor / oldScaling;

                        if (dScale > 0 && dScale.IsNumber())
                        {

                            _mouseSelection = Extensions.Multiply(_mouseSelection, dScale);

                            UpdateSelectionBorder();
                        }
                    }
                }
            }            
        }

        private void selectionBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (MoveSelection)
            {
                _lastMousePosition = e.GetPosition(this);

                _isMoving = true;

                if (_selectionBorder != null)
                {
                    _selectionBorder.CaptureMouse();
                }

                e.Handled = true;
            }
        }

        private void selectionBorder_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isMoving)
            {
                Point mousePosition = e.GetPosition(this);

                double xd = mousePosition.X - _lastMousePosition.X;
                double yd = mousePosition.Y - _lastMousePosition.Y;

                Point position1 = new Point(xd + _mouseSelection.Left,
                                            yd + _mouseSelection.Top);
                Point position2 = new Point(xd + _mouseSelection.Right,
                                            yd + _mouseSelection.Bottom);

                UpdateSelection(position1, position2);

                _lastMousePosition = mousePosition;
            }
        }

        private void selectionBorder_LostMouseCapture(object sender, MouseEventArgs e)
        {
            if (_isMoving)
            {
                _isMoving = false;

                if (_selectionBorder != null)
                {
                    _selectionBorder.ReleaseMouseCapture();
                }
            }
        }

        private void selectionArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!MoveSelection)
            {
                _lastMousePosition = e.GetPosition(_imageElement);

                if (SelectionMode == ImageEditorSelectionMode.FixedSize)
                {
                    Point secondPosition = new Point(_lastMousePosition.X + _scaleFactor * RequiredSelectionWidth,
                                                     _lastMousePosition.Y + _scaleFactor * RequiredSelectionHeight);

                    UpdateSelection(_lastMousePosition, secondPosition);
                }
                else
                {
                    UpdateSelection(new Point(0, 0), new Point(0, 0));

                    _isSelecting = true;

                    if (_selectionArea != null)
                    {
                        _selectionArea.CaptureMouse();
                    }
                }

                e.Handled = true;
            }
        }

        private void selectionArea_MouseMove(object sender, MouseEventArgs e)
        {
            if (_selectionArea != null)
            {
                if (_isSelecting)
                {
                    Point mousePosition = e.GetPosition(_imageElement);

                    if (SelectionMode == ImageEditorSelectionMode.FixedRatio)
                    {
                        if (RequiredSelectionWidth > 0 &&
                            RequiredSelectionHeight > 0)
                        {
                            double xd = mousePosition.X - _lastMousePosition.X;

                            mousePosition.Y = _lastMousePosition.Y + xd * RequiredSelectionHeight / RequiredSelectionWidth;

                            UpdateSelection(mousePosition, _lastMousePosition);
                        }
                    }
                    else if (SelectionMode == ImageEditorSelectionMode.Normal)
                    {
                        UpdateSelection(mousePosition, _lastMousePosition);
                    }
                }
            }
        }

        private void selectionArea_LostMouseCapture(object sender, MouseEventArgs e)
        {
            if (_isSelecting)
            {
                _isSelecting = false;

                if (_selectionArea != null)
                {
                    _selectionArea.ReleaseMouseCapture();
                }
            }
        }

        private void UpdateSelection(Point firstPoint, Point secondPoint)
        {
            double xmin = Math.Min(firstPoint.X, secondPoint.X);
            double xmax = Math.Max(firstPoint.X, secondPoint.X);
            double ymin = Math.Min(firstPoint.Y, secondPoint.Y);
            double ymax = Math.Max(firstPoint.Y, secondPoint.Y);

            _mouseSelection = new Rect(xmin, ymin, xmax - xmin, ymax - ymin);

            UpdateSelection();
        }

        private void UpdateSelection()
        {
            if (Source != null && _scaleFactor > 0)
            {
                double xmin = _mouseSelection.Left;
                double xmax = _mouseSelection.Right;
                double ymin = _mouseSelection.Top;
                double ymax = _mouseSelection.Bottom;

                xmin = (xmin / _scaleFactor).RemainBetween(0, Source.PixelWidth);
                xmax = (xmax / _scaleFactor).RemainBetween(0, Source.PixelWidth);

                ymin = (ymin / _scaleFactor).RemainBetween(0, Source.PixelHeight);
                ymax = (ymax / _scaleFactor).RemainBetween(0, Source.PixelHeight);

                Selection = new Rect(xmin, ymin, xmax - xmin, ymax - ymin);
            }
        }

        private void UpdateSelectionBorder()
        {
            if (_selectionBorder != null)
            {
                _selectionBorder.Margin = new Thickness(Selection.X * _scaleFactor, Selection.Y * _scaleFactor, 0, 0);

                _selectionBorder.Width  = Selection.Width  * _scaleFactor;
                _selectionBorder.Height = Selection.Height * _scaleFactor;
            }
        }

        #endregion
    }
}
