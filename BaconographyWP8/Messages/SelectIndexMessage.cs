using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Messages;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyWP8.Messages
{
    public class SelectIndexMessage : MessageBase
    {
        public Type TypeContext { get; set; }
		public int Index { get; set; }
    }
}
