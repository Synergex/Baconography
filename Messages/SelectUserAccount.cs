using GalaSoft.MvvmLight.Messaging;
using Baconography.RedditAPI;
using Baconography.RedditAPI.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.Messages
{
    class SelectUserAccount : MessageBase
    {
        public TypedThing<Account> Account { get; set; }
    }
}
