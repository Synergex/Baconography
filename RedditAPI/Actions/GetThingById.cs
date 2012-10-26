using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.RedditAPI.Actions
{
    public class GetThingById
    {
        public string Id { get; set; }

        public async Task<Thing> Run()
        {
            var targetUri = string.Format("http://www.reddit.com/by_id/{0}.json", Id);

            try
            {
                var thingStr = await User.UnAuthedGet(targetUri);
                return JsonConvert.DeserializeObject<Thing>(thingStr);
            }
            catch (Exception)
            {
                User.ShowDisconnectedMessage();
                return null;
            }
        }
    }
}
