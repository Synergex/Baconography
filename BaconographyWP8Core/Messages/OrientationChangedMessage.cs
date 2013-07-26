using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyWP8.Messages
{
	public class OrientationChangedMessage : MessageBase
    {
		public PageOrientation Orientation { get; set; }
    }
}
