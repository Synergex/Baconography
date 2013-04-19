// ===============================================================================
// App.cs
// .NET Image Tools
// ===============================================================================
// Copyright (c) .NET Image Tools Development Group. 
// All rights reserved.
// ===============================================================================

using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;

namespace ImageTools.Demos.Phone
{
    /// <summary>
    /// Entry class of the demo application.
    /// </summary>
    public partial class App : Application
    {
        #region Fields

        private bool _isPhoneApplicationInitialized = false;

        #endregion

        #region Properties

        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public PhoneApplicationFrame RootFrame { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// </summary>
        public App()
        {
            UnhandledException += Application_UnhandledException;

            if (Debugger.IsAttached)
            {
                Application.Current.Host.Settings.EnableFrameRateCounter = true;
            }

            InitializeComponent();
            InitializePhoneApplication();
        }

        #endregion

        #region Methods

        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
        }

        private void InitializePhoneApplication()
        {
            if (!_isPhoneApplicationInitialized)
            {
                RootFrame = new PhoneApplicationFrame();
                RootFrame.Navigated += new NavigatedEventHandler(RootFrame_Navigated);
                RootFrame.NavigationFailed += RootFrame_NavigationFailed;

                _isPhoneApplicationInitialized = true;
            }
        }

        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
        }

        private void RootFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (RootVisual != RootFrame)
            {
                RootVisual = RootFrame;
            }

            RootFrame.Navigated -= new NavigatedEventHandler(RootFrame_Navigated); ;
        }

        #endregion
    }
}