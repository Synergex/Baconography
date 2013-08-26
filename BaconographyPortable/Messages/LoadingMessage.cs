using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Messages
{
    public class LoadingMessage : MessageBase
    {
        public bool Loading { get; set; }
        public string Message { get; set; }
        public uint? Percentage { get; set; }
    }
}
