// ===============================================================================
// IBarcodeReaderContract.cs
// .NET Image Tools
// ===============================================================================
// Copyright (c) .NET Image Tools Development Group. 
// All rights reserved.
// ===============================================================================

using System;
using System.Diagnostics.Contracts;

namespace ImageTools
{
    [ContractClassFor(typeof(IBarcodeReader))]
    abstract class IBarcodeReaderContract : IBarcodeReader
    {
        BarcodeResult IBarcodeReader.ReadBarcode(ExtendedImage image)
        {
            Contract.Requires<ArgumentNullException>(image != null, "Image cannot be null.");
            Contract.Requires<ArgumentException>(image.IsFilled, "Image cannot be empty.");
            throw new NotImplementedException();
        }
    }
}
