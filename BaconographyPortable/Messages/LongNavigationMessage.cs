﻿using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Messages
{
    public class LongNavigationMessage : MessageBase
    {
        public bool Finished { get; set; }
        public string TargetUrl { get; set; }
    }
}
