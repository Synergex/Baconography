using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Messages
{
    public class OfflineStatusMessage : MessageBase
    {
        public enum OfflineStatus
        {
            None,
            Initial,
            TopComments,
            AllComments,
            Thumnail,
            Content
        }

        public string LinkId { get; set; }
        public OfflineStatus Status { get; set; }
    }
}
