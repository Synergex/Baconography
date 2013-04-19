// ===============================================================================
// AnimationMessageInfo.cs
// .NET Image Tools
// ===============================================================================
// Copyright (c) .NET Image Tools Development Group. 
// All rights reserved.
// ===============================================================================

using System;
using System.Windows.Media;
using GalaSoft.MvvmLight;

namespace ImageTools.Demos.Views
{
    /// <summary>
    /// Stores the information about one message of our sample chat.
    /// </summary>
    public class AnimationMessageInfo : ViewModelBase
    {
        #region Properties

        private DateTime _created;
        /// <summary>
        /// Gets or sets the the date and time when this message info object was created.
        /// </summary>
        /// <value>The date and time when this message info object was created.</value>
        public DateTime Created
        {
            get { return _created; }
            set
            {
                if (_created != value)
                {
                    _created = value;
                    RaisePropertyChanged("Created");
                }
            }
        }

        private string _message;
        /// <summary>
        /// Gets or sets the text of the chat message.
        /// </summary>
        /// <value>The text of the message.</value>
        public string Message
        {
            get { return _message; }
            set
            {
                if (_message != value)
                {
                    _message = value;
                    RaisePropertyChanged("Message");
                }
            }
        }

        private Brush _messageBrush;
        /// <summary>
        /// Gets or sets the brush that is used to render the message.
        /// </summary>
        /// <value>The brush that is used to render the message.</value>
        public Brush MessageBrush
        {
            get { return _messageBrush; }
            set
            {
                if (_messageBrush != value)
                {
                    _messageBrush = value;
                    RaisePropertyChanged("MessageBrush");
                }
            }
        }

        private string _user;
        /// <summary>
        /// Gets or sets the name of the user, which has sent this message with the sample chat.
        /// </summary>
        /// <value>The name of the user, which has sent this message.</value>
        public string User
        {
            get { return _user; }
            set
            {
                if (_user != value)
                {
                    _user = value;
                    RaisePropertyChanged("User");
                }
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AnimationMessageInfo"/> class.
        /// </summary>
        public AnimationMessageInfo()
        {
            Created = DateTime.Now;
        }

        #endregion
    }
}
