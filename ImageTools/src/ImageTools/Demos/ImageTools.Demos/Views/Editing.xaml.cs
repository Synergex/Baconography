// ===============================================================================
// Editing.cs
// .NET Image Tools
// ===============================================================================
// Copyright (c) .NET Image Tools Development Group. 
// All rights reserved.
// ===============================================================================

using System;
using System.Windows;
using System.Windows.Controls;
using ImageTools.Controls;
using ImageTools.Filtering;

namespace ImageTools.Demos.Views
{
    /// <summary>
    /// This sample shows how to use the editor container to write a image editor in silverlight.
    /// This control can be used in an image uploader where user can edit an image before sending it to 
    /// the server.
    /// </summary>
    public partial class Editing : Page
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Editing"/> class and loads a sample 
        /// image that is edited by the editor container.
        /// </summary>
        public Editing()
        {
            InitializeComponent();

            SelectionModeComboBox.SelectedIndex = 2;

            Container.Source = new ExtendedImage();
            Container.Source.UriSource = new Uri("Images/Penguins.jpg", UriKind.Relative);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Handles the SelectionChanged event of the SelectionModeComboBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void SelectionModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectionModeComboBox != null)
            {
                string value = SelectionModeComboBox.SelectedItem.ToString();

                switch (value)
                {
                    case "Free Selection":
                        Container.SelectionMode = ImageEditorSelectionMode.Normal;
                        SelectionWidthTextBox.IsEnabled = false;
                        SelectionHeightTextBox.IsEnabled = false;
                        break;
                    case "Fixed Size":
                        Container.SelectionMode = ImageEditorSelectionMode.FixedSize;
                        SelectionWidthTextBox.IsEnabled = true;
                        SelectionHeightTextBox.IsEnabled = true;
                        break;
                    case "Fixed Ratio":
                        Container.SelectionMode = ImageEditorSelectionMode.FixedRatio;
                        SelectionWidthTextBox.IsEnabled = true;
                        SelectionHeightTextBox.IsEnabled = true;
                        break;
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the RotateCounterClockwiseButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void RotateCounterClockwiseButton_Click(object sender, RoutedEventArgs e)
        {
            if (Container.Source != null && Container.Source.IsFilled)
            {
                Container.Source = ExtendedImage.Transform(Container.Source, RotationType.Rotate270, FlippingType.None);
            }
        }

        /// <summary>
        /// Handles the Click event of the RotateClockwiseButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void RotateClockwiseButton_Click(object sender, RoutedEventArgs e)
        {
            if (Container.Source != null && Container.Source.IsFilled)
            {
                Container.Source = ExtendedImage.Transform(Container.Source, RotationType.Rotate90, FlippingType.None);
            }
        }

        /// <summary>
        /// Handles the Click event of the FlipHorizontalButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void FlipHorizontalButton_Click(object sender, RoutedEventArgs e)
        {
            if (Container.Source != null && Container.Source.IsFilled)
            {
                Container.Source = ExtendedImage.Transform(Container.Source, RotationType.None, FlippingType.FlipX);
            }
        }

        /// <summary>
        /// Handles the Click event of the FlipVerticalButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void FlipVerticalButton_Click(object sender, RoutedEventArgs e)
        {
            if (Container.Source != null && Container.Source.IsFilled)
            {
                Container.Source = ExtendedImage.Transform(Container.Source, RotationType.None, FlippingType.FlipY);
            }
        }

        /// <summary>
        /// Handles the Click event of the ChangeColorsButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ChangeColorsButton_Click(object sender, RoutedEventArgs e)
        {
            if (Container.Source != null && Container.Source.IsFilled)
            {
                IImageFilter[] filters = 
                    new IImageFilter[] { new Brightness((int)BrightnessSlider.Value), new Contrast((int)ContrastSlider.Value) };

                Container.Source = ExtendedImage.ApplyFilters(Container.Source, filters);
            }
        }

        /// <summary>
        /// Handles the Click event of the ScaleInButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ScaleInButton_Click(object sender, RoutedEventArgs e)
        {
            Container.ScalingMode = ImageEditorScalingMode.FixedScaling;
            Container.Scaling = Math.Max(0.1, Math.Round(Container.Scaling + 0.1, 2));
        }

        /// <summary>
        /// Handles the Click event of the ScaleOutButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ScaleOutButton_Click(object sender, RoutedEventArgs e)
        {
            Container.ScalingMode = ImageEditorScalingMode.FixedScaling;
            Container.Scaling = Math.Max(0.1, Math.Round(Container.Scaling - 0.1, 2));
        }

        /// <summary>
        /// Handles the Click event of the ScalePageButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ScalePageButton_Click(object sender, RoutedEventArgs e)
        {
            Container.ScalingMode = ImageEditorScalingMode.Auto;
        }

        #endregion
    }
}
