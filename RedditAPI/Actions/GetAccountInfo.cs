using Newtonsoft.Json;
using Baconography.RedditAPI.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.RedditAPI.Actions
{
    public class GetAccountInfo
    {
        public string AccountName { get; set; }

        public async Task<TypedThing<Account>> Run()
        {
            var targetUri = string.Format("http://www.reddit.com/user/{0}/about.json", AccountName);

            try
            {
                var account = await User.UnAuthedGet(targetUri);
                return new TypedThing<Account>(JsonConvert.DeserializeObject<Thing>(account));
            }
            catch (Exception)
            {
                User.ShowDisconnectedMessage();
                return new TypedThing<Account>(new Thing { Kind = "t3", Data = new Account { Name = AccountName } });
            }
        }
    }
}
