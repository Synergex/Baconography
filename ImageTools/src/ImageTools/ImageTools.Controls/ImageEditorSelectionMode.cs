// ===============================================================================
// ImageEditorSelectionMode.cs
// .NET Image Tools
// ===============================================================================
// Copyright (c) .NET Image Tools Development Group. 
// All rights reserved.
// ===============================================================================

namespace ImageTools.Controls
{
    /// <summary>
    /// Defines the selection mode of the image editor.
    /// </summary>
    public enum ImageEditorSelectionMode
    {
        /// <summary>
        /// Default selection mode.
        /// </summary>
        Normal,
        /// <summary>
        /// The size and width of the selected area is fixed.
        /// </summary>
        FixedSize,
        /// <summary>
        /// The ratio between height and width of the selection area is fixed.
        /// </summary>
        FixedRatio
    }
}
