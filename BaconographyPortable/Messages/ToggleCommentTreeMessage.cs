using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Messages;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BaconographyPortable.ViewModel;

namespace BaconographyPortable.Messages
{
    public class ToggleCommentTreeMessage : MessageBase
    {
		public CommentViewModel CommentViewModel { get; set; }
    }
}
