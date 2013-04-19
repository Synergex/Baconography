// ===============================================================================
// Presentation.cs
// .NET Image Tools
// ===============================================================================
// Copyright (c) .NET Image Tools Development Group. 
// All rights reserved.
// ===============================================================================

using System;
using System.Windows.Controls;
using ImageTools.Filtering;

namespace ImageTools.Demos.Views
{
    /// <summary>
    /// This page displays some images with fitlers and title and description about this images.
    /// </summary>
    /// <remarks>This sample shows how easy it is to use imagetools in xaml. Use normal dependency properties 
    /// and define the filters and parameters in xaml. Thee images are loaded asynchronously, so dont care about 
    /// performance issues at startup time.</remarks>
    public partial class Presentation : Page
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Presentation"/> class, loads the sample images from 
        /// resources and apply them to the picture controls.
        /// </summary>
        public Presentation()
        {
            InitializeComponent();

            ExtendedImage desert = new ExtendedImage();
            // The extended image can directly handle relative paths to embedded images.
            // Dont use the approach you would use when loading a silverlight image from a resource.
            desert.UriSource = new Uri("/Images/Desert.jpg", UriKind.Relative);
            desert.LoadingFailed += new EventHandler<UnhandledExceptionEventArgs>(desert_LoadingFailed);
            desert.LoadingCompleted += new EventHandler(image_LoadingCompleted);

            DesertFilterImage1.Image = desert;
            DesertFilterImage2.Image = desert;
            DesertFilterImage3.Image = desert;
            DesertFilterImage4.Image = desert;

            ExtendedImage building = new ExtendedImage();
            building.UriSource = new Uri("/Images/Building.png", UriKind.Relative);

            BuildingFilterImage1.Image = building;
            BuildingFilterImage2.Image = building;
            BuildingFilterImage3.Image = building;
            BuildingFilterImage4.Image = building;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Handles the LoadingFailed event of the desert control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.UnhandledExceptionEventArgs"/> instance containing the event data.</param>
        private void desert_LoadingFailed(object sender, UnhandledExceptionEventArgs e)
        {
        }

        /// <summary>
        /// Handles the LoadingCompleted event of the image.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void image_LoadingCompleted(object sender, EventArgs e)
        {
            ExtendedImage image = sender as ExtendedImage;

            if (image != null)
            {
                Dispatcher.BeginInvoke(() =>
                    {
                        BuildingFilterImage4.Filter = new BlendingFilter(image) { GlobalAlphaFactor = 0.5 };

                        ResizeImage1.Image = image;
                        ResizeImage2.Image = ExtendedImage.Resize(image, 900, new BilinearResizer());
                        ResizeImage3.Image = ExtendedImage.Resize(image, 100, new BilinearResizer());
                        ResizeImage4.Image = ExtendedImage.Resize(image, 100, new BilinearResizer());
                    });
            }
        }

        #endregion
    }
}
