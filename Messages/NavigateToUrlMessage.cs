using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.Messages
{
    class NavigateToUrlMessage : MessageBase
    {
        public string TargetUrl { get; set; }
        public string Title { get; set; }
    }
}
