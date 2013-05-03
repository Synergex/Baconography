// ===============================================================================
// ImageConverter.cs
// .NET Image Tools
// ===============================================================================
// Copyright (c) .NET Image Tools Development Group. 
// All rights reserved.
// ===============================================================================

using System;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Globalization;
using System.Windows;
using ImageTools;
using System.IO;

namespace ImageTools.Controls
{
    /// <summary>
    /// Converts images that are defined by paths as strings or uris or defined as 
    /// images to an extended image to bind it to an animated image.
    /// </summary>
    public class ImageConverter : IValueConverter
    {
        #region IValueConverter Members

        /// <summary>
        /// Modifies the source data before passing it to the target for display in the UI.
        /// </summary>
        /// <param name="value">The source data being passed to the target.</param>
        /// <param name="targetType">The <see cref="T:System.Type"/> of data expected by the target dependency property.</param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <param name="culture">The culture of the conversion.</param>
        /// <returns>
        /// The value to be passed to the target dependency property.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            object result = DependencyProperty.UnsetValue;

            if (value != null)
            {
                var sourceAsUri = value as Uri;
                if (sourceAsUri != null)
                {
                    ExtendedImage extendedImage = new ExtendedImage();
                    extendedImage.UriSource = sourceAsUri;

                    return extendedImage;
                }

                var sourceAsString = value as String;
                if (sourceAsString != null)
                {
                    ExtendedImage extendedImage = new ExtendedImage();
                    extendedImage.UriSource = new Uri(sourceAsString, UriKind.RelativeOrAbsolute);

                    return extendedImage;
                }

                var sourceAsStream = value as Stream;
                if (sourceAsStream != null)
                {
                    ExtendedImage extendedImage = new ExtendedImage();
                    extendedImage.SetSource(sourceAsStream);

                    return extendedImage;
                }

                var sourceAsWriteableBitmap = value as WriteableBitmap;
                if (sourceAsWriteableBitmap != null)
                {
                    return sourceAsWriteableBitmap.ToImage();
                }

                var sourceAsExtendedImage = value as ExtendedImage;
                if (sourceAsExtendedImage != null)
                {
                    return sourceAsExtendedImage;
                }
            }

            return result;
        }

        /// <summary>
        /// Modifies the target data before passing it to the source object.  This method is called only in <see cref="F:System.Windows.Data.BindingMode.TwoWay"/> bindings.
        /// </summary>
        /// <param name="value">The target data being passed to the source.</param>
        /// <param name="targetType">The <see cref="T:System.Type"/> of data expected by the source object.</param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <param name="culture">The culture of the conversion.</param>
        /// <returns>
        /// The value to be passed to the source object.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
