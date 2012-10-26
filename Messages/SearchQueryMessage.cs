using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.Messages
{
    public class SearchQueryMessage : MessageBase
    {
        public string Query { get; set; }
    }
}
