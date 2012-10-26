using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.Messages
{
    public class ConnectionStatusMessage
    {
        public bool UserInitiated { get; set; }
        public bool IsOnline { get; set; }
    }
}
