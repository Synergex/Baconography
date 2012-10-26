using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.RedditAPI.Actions
{
    class AddSubredditSubscription : IRedditAction
    {
        public string Subreddit { get; set; }
        public bool Unsub { get; set; }

        public async void Run(User loggedInUser)
        {
            var modhash = (string)loggedInUser.Me.ModHash;
            await loggedInUser.SendPost(string.Format("sr={0}&uh={1}&r={2}&renderstyle={3}&action={4}",
                Subreddit, modhash, Subreddit, "html", Unsub ? "unsub" : "sub" ),
                "http://www.reddit.com/api/subscribe");
        }
    }
}
