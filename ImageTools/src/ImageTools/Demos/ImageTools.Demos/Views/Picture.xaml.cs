// ===============================================================================
// Picture.cs
// .NET Image Tools
// ===============================================================================
// Copyright (c) .NET Image Tools Development Group. 
// All rights reserved.
// ===============================================================================

using System.Windows;
using System.Windows.Controls;

namespace ImageTools.Demos.Views
{
    /// <summary>
    /// This control renders an image with an applied fitler and a title and description that contains 
    /// some information about this image.
    /// </summary>
    public partial class Picture : UserControl
    {
        #region Dependency Properties

        /// <summary>
        /// Defines the <see cref="Image"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(Picture), new PropertyMetadata("Title"));
        /// <summary>
        /// Gets or sets a title of this picture.
        /// </summary>
        /// <value>The title of this picture.</value>
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        /// <summary>
        /// Defines the <see cref="Image"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(string), typeof(Picture), new PropertyMetadata("Description"));
        /// <summary>
        /// Gets or sets a description about the image and the filter.
        /// </summary>
        /// <value>The description about the image and the filter.</value>
        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        /// <summary>
        /// Defines the <see cref="Image"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FilterProperty =
            DependencyProperty.Register("Filter", typeof(IImageFilter), typeof(Picture), new PropertyMetadata(null));
        /// <summary>
        /// Gets or sets the filter that is applied to the image before it is rendered.
        /// </summary>
        /// <value>The filter that is applied to the image before it is rendered.</value>
        public IImageFilter Filter
        {
            get { return (IImageFilter)GetValue(FilterProperty); }
            set { SetValue(FilterProperty, value); }
        }

        /// <summary>
        /// Defines the <see cref="Image"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register("Image", typeof(ExtendedImage), typeof(Picture), new PropertyMetadata(null));
        /// <summary>
        /// Gets or sets the rendered image.
        /// </summary>
        /// <value>The image that is rendered by this control.</value>
        public ExtendedImage Image
        {
            get { return (ExtendedImage)GetValue(ImageProperty); }
            set { SetValue(ImageProperty, value); }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Picture"/> class.
        /// </summary>
        public Picture()
        {
            InitializeComponent();
        }

        #endregion
    }
}
