using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Messages
{
    class PickerFileMessage : MessageBase
    {
        public string TargetUrl { get; set; }
        public bool Selected { get; set; }
    }
}
