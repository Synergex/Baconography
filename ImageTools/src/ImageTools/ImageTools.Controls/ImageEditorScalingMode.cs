// ===============================================================================
// ImageEditorScalingMode.cs
// .NET Image Tools
// ===============================================================================
// Copyright (c) .NET Image Tools Development Group. 
// All rights reserved.
// ===============================================================================

namespace ImageTools.Controls
{
    /// <summary>
    /// Defines the behavior of the the image editor to scale the image.
    /// </summary>
    public enum ImageEditorScalingMode
    {
        /// <summary>
        /// The scaling of the image is fix and has a predefined value.
        /// </summary>
        FixedScaling,
        /// <summary>
        /// The image is calculated to make the image fit to the width 
        /// and height of the editor but to keep the ratio of the image.
        /// </summary>
        Auto
    }
}
