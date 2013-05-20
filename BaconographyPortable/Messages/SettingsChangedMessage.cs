﻿using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Messages
{
	public class SettingsChangedMessage : MessageBase
    {
		public bool InitialLoad { get; set; }
    }
}
