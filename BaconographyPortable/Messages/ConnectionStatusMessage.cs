using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Messages
{
    public class ConnectionStatusMessage : MessageBase
    {
        public bool UserInitiated { get; set; }
        public bool IsOnline { get; set; }
    }
}
