using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Messages;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Messages
{
    public class SelectTemporaryRedditMessage : SelectSubredditMessage
    {
		public bool IsTemp
		{
			get { return true; }
		}
    }
}
