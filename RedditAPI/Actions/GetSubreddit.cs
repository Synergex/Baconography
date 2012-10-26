using Newtonsoft.Json;
using Baconography.RedditAPI.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.RedditAPI.Actions
{
    class GetSubreddit
    {
        public string Name { get; set; }

        public async Task<TypedThing<Subreddit>> Run()
        {
            var targetUri = string.Format("http://www.reddit.com/r/{0}/about.json", Name);
            try
            {
                var comments = await User.UnAuthedGet(targetUri);
                return new TypedThing<Subreddit>(JsonConvert.DeserializeObject<Thing>(comments));
            }
            catch (Exception)
            {
                User.ShowDisconnectedMessage();
                return new TypedThing<Subreddit>(new Thing { Kind = "t5", Data = new Subreddit { Headertitle = Name } });
            }
        }
    }
}
