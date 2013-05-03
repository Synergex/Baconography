using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyW8.Common
{
    class PageChangeMessage : MessageBase
    {
        public bool Forward { get; set; }
    }
}
