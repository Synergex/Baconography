// ===============================================================================
// MainPage.cs
// .NET Image Tools
// ===============================================================================
// Copyright (c) .NET Image Tools Development Group. 
// All rights reserved.
// ===============================================================================

using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using ImageTools.Controls;

namespace ImageTools.Demos
{
    /// <summary>
    /// Defines the main view of the application that is responsible of rendering the links and the 
    /// content frame.
    /// </summary>
    public partial class MainPage : UserControl
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage()
        {
            InitializeComponent();

            var a = typeof(ExtendedImage).Assembly;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Handles the Navigated event of the ContentFrame control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Navigation.NavigationEventArgs"/> instance containing the event data.</param>
        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            foreach (UIElement child in LinksStackPanel.Children)
            {
                HyperlinkButton hb = child as HyperlinkButton;
                if (hb != null && hb.NavigateUri != null)
                {
                    if (ContentFrame.UriMapper.MapUri(e.Uri).ToString().Equals(ContentFrame.UriMapper.MapUri(hb.NavigateUri).ToString()))
                    {
                        VisualStateManager.GoToState(hb, "ActiveLink", true);
                    }
                    else
                    {
                        VisualStateManager.GoToState(hb, "InactiveLink", true);
                    }
                }
            }
        }

        /// <summary>
        /// Handles the NavigationFailed event of the ContentFrame control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Navigation.NavigationFailedEventArgs"/> instance containing the event data.</param>
        private void ContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            e.Handled = true;
            MessageBox.Show(string.Format(CultureInfo.CurrentCulture, "Cannot navigate to {0}.", e.Uri));
        }

        #endregion
    }
}