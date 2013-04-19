// ===============================================================================
// App.cs
// .NET Image Tools
// ===============================================================================
// Copyright (c) .NET Image Tools Development Group. 
// All rights reserved.
// ===============================================================================

using System.Diagnostics;
using System.Windows;
using ImageTools.IO;
using ImageTools.IO.Bmp;
using ImageTools.IO.Gif;
using ImageTools.IO.Jpeg;
using ImageTools.IO.Png;

namespace ImageTools.Demos
{
    /// <summary>
    /// Entry class of the demo application.
    /// </summary>
    public partial class App : Application
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// </summary>
        /// <remarks>Registers all available image decoders that can be 
        /// used in the whole application.</remarks>
        public App()
        {
            Startup += Application_Startup;

            UnhandledException += this.Application_UnhandledException;

            Decoders.AddDecoder<PngDecoder>();
            Decoders.AddDecoder<JpegDecoder>();
            Decoders.AddDecoder<BmpDecoder>();
            Decoders.AddDecoder<GifDecoder>();

            InitializeComponent();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Handles the Startup event of the Silverlight Application.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.StartupEventArgs"/> instance containing the event data.</param>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            RootVisual = new MainPage();
        }

        /// <summary>
        /// Handles the UnhandledException event of the Silverlight Application.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.ApplicationUnhandledExceptionEventArgs"/> instance containing the event data.</param>
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (!Debugger.IsAttached)
            {
                e.Handled = true;

                MessageBox.Show(e.ExceptionObject.ToString());
            }
        }

        #endregion
    }
}