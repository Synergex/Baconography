using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.RedditAPI.Actions
{
    class AddPost : IRedditAction
    {
        public string Kind { get; set; }
        public string Url { get; set; }
        public string SubReddit { get; set; }
        public string Title { get; set; }

        public async void Run(User loggedInUser)
        {
            var modhash = (string)loggedInUser.Me.ModHash;
            await loggedInUser.SendPost(string.Format("uh={0}&kind={1}&url={2}&sr={3}&title={4}&r={3}&renderstyle=html",
                modhash, Kind, Url, SubReddit, Title), 
                "http://www.reddit.com/api/submit");
        }
    }
}
