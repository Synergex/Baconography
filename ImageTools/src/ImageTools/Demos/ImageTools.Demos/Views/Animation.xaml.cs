// ===============================================================================
// Animation.cs
// .NET Image Tools
// ===============================================================================
// Copyright (c) .NET Image Tools Development Group. 
// All rights reserved.
// ===============================================================================

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ImageTools.Demos.Views
{
    /// <summary>
    /// This sample shows how to use animated images like animated smilies in your application.
    /// A typical example is a chat, like it is shown here in the sample page.
    /// </summary>
    public partial class Animation : Page
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Animation"/> class.
        /// </summary>
        public Animation()
        {
            InitializeComponent();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Handles the Click event of the SendButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            string message = InputTextBox.Text;

            if (!string.IsNullOrEmpty(message))
            {
                // Create a new message info object and fill it with some data. This message info object is converted 
                // to textblock controls and animated image using a converter.

                AnimationMessageInfo messageInfo = new AnimationMessageInfo();
                messageInfo.Created = DateTime.Now;
                messageInfo.Message = message;
                messageInfo.MessageBrush = new SolidColorBrush(Colors.Green);
                messageInfo.User = "Developer";

                HistoryListBox.Items.Add(messageInfo);

                InputTextBox.Text = string.Empty;
            }
        }

        #endregion
    }
}
