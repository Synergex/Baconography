using BaconographyPortable.Model.Reddit;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Messages
{
    public class SelectUserAccountMessage : MessageBase
    {
        [JsonIgnore]
        public TypedThing<Account> Account { get; set; }
        public Thing UntypedAccount
        {
            get
            {
                return Account;
            }
            set
            {
                Account = new TypedThing<Account>(value);
            }
        }
    }
}
