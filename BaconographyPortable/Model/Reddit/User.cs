using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit
{
    [DataContract]
    public class User
    {
        public string Username { get; set; }
        public string LoginCookie { get; set; }
        public bool Authenticated { get; set; }
        public Account Me { get; set; }
    }
}
