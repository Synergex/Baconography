// ===============================================================================
// MainPageViewModel.cs
// .NET Image Tools
// ===============================================================================
// Copyright (c) .NET Image Tools Development Group. 
// All rights reserved.
// ===============================================================================

using System;
using ImageTools.IO;
using ImageTools.IO.Png;

namespace ImageTools.Demos.Phone
{
    /// <summary>
    /// Simple view model that holds a property to the image source.
    /// </summary>
    public sealed class MainPageViewModel
    {
        private readonly Uri _imageSource = new Uri("Images/Building.png", UriKind.Relative);
        /// <summary>
        /// Gets or sets the path to the source image.
        /// </summary>
        /// <value>The path to the source image.</value>
        public Uri ImageSource
        {
            get { return _imageSource; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPageViewModel"/> class.
        /// </summary>
        public MainPageViewModel()
        {
            Decoders.AddDecoder<PngDecoder>();
        }
    }
}
