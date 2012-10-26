using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.Messages
{
    public class PickerFileMessage : MessageBase
    {
        public string TargetUrl { get; set; }
        public bool Selected { get; set; }
    }
}
