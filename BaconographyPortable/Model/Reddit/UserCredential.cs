using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Model.Reddit
{
    [DataContract]
    public class UserCredential
    {
        public string LoginCookie { get; set; }
        public string Username { get; set; }
        public Thing Me { get; set; }
        public bool IsDefault { get; set; }
    }
}
