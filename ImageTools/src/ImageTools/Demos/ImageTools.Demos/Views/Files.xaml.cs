// ===============================================================================
// Files.cs
// .NET Image Tools
// ===============================================================================
// Copyright (c) .NET Image Tools Development Group. 
// All rights reserved.
// ===============================================================================

using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using ImageTools.Filtering;
using System;

namespace ImageTools.Demos.Views
{
    /// <summary>
    /// This sample shows how to load images from files using dialogs and how to convert an canvas to
    /// an extended image and to write it as png or jpeg image to a file.
    /// </summary>
    public partial class Files : Page
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Files"/> class.
        /// </summary>
        public Files()
        {
            InitializeComponent();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Handles the Click event of the LoadImageButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void LoadImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = false;

            // Define the filter to load all format that can be handled by image tools.
            openFileDialog.Filter = "Image Files (*.jpg;*.png;*.bmp;*gif)|*.jpg;*.png;*.bmp;*.gif";

            if (openFileDialog.ShowDialog() == true)
            {
                FileInfo fileInfo = openFileDialog.File;

                ExtendedImage extendedImage = new ExtendedImage();
                extendedImage.SetSource(fileInfo.OpenRead());

                Image.Source = extendedImage;
            }
        }

        /// <summary>
        /// Handles the Click event of the SaveCreenButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SaveScreenButton_Click(object sender, RoutedEventArgs e)
        {
            // Use extension methods to convert any framework element to an extended 
            // image instance.
            ExtendedImage extendedImage = Area.ToImage();

            SaveFileDialog saveFileDialog = new SaveFileDialog();

            // Open a file stream using the save file dialog. Only allow defining png or jpeg files. 
            // Gif files are not supported by image tools and bmp makes no sense in the most scenarios.
            saveFileDialog.Filter = "Image Files (*.png, *.jpg)|*.png;*.jpg";

            if (saveFileDialog.ShowDialog() == true)
            {
                using (Stream stream = saveFileDialog.OpenFile())
                {
                    // Use the write to stream extension method to write the image to the specified stream.
                    // The image encoder is selected by the extension of the name of the image.
                    extendedImage.WriteToStream(stream, saveFileDialog.SafeFileName);
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the ScanBarcodeButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ScanBarcodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (Image.Source != null &&
                Image.Source.IsFilled)
            {
                IBarcodeReader barcodeReader = new ZXingBarcodeReader(true, BinarizerMode.Hybrid);

                BarcodeResult result = barcodeReader.ReadBarcode(Image.Source);

                if (result != null)
                {
                    BarcodeTextTextBox.Text   = result.Text;
                    BarcodeFormatTextBox.Text = result.Format.ToString();
                }
                else
                {
                    BarcodeTextTextBox.Text   = "Barcode not detected!";
                    BarcodeFormatTextBox.Text = "Barcode not detected!";
                }
            }
        }

        #endregion
    }
}
