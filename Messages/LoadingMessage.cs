using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.Messages
{
    class LoadingMessage : MessageBase
    {
        public bool Loading { get; set; }
    }
}
