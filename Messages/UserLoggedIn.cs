using GalaSoft.MvvmLight.Messaging;
using Baconography.RedditAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.Messages
{
    class UserLoggedIn : MessageBase
    {
        public User CurrentUser { get; set; }
    }
}
