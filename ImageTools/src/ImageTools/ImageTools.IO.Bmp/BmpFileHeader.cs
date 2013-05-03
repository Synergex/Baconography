﻿// ===============================================================================
// BmpFileHeader.cs
// .NET Image Tools
// ===============================================================================
// Copyright (c) .NET Image Tools Development Group. 
// All rights reserved.
// ===============================================================================

namespace ImageTools.IO.Bmp
{
    /// <summary>
    /// Stores general information about the Bitmap file.
    /// </summary>
    /// <remarks>
    /// The first two bytes of the Bitmap file format
    /// (thus the Bitmap header) are stored in big-endian order.
    /// All of the other integer values are stored in little-endian format
    /// (i.e. least-significant byte first).
    /// </remarks>
    class BmpFileHeader
    {
        /// <summary>
        /// Defines of the data structure in the bitmap file.
        /// </summary>
        public const int Size = 14;

        /// <summary>
        /// The magic number used to identify the bitmap file: 0x42 0x4D 
        /// (Hex code points for B and M)
        /// </summary>
        public short Type;
        /// <summary>
        /// The size of the bitmap file in bytes.
        /// </summary>
        public int FileSize;
        /// <summary>
        /// Reserved; actual value depends on the application 
        /// that creates the image.
        /// </summary>
        public int Reserved;
        /// <summary>
        /// The offset, i.e. starting address, of the byte where 
        /// the bitmap data can be found.
        /// </summary>
        public int Offset;
    }
}
