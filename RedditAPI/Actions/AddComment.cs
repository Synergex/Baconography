using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.RedditAPI.Actions
{
    class AddComment : IRedditAction
    {
        public string ParentId { get; set; }
        public string Content { get; set; }

        public async void Run(User loggedInUser)
        {
            var modhash = (string)loggedInUser.Me.ModHash;
            await loggedInUser.SendPost(string.Format("thing_id={0}&text={1}&uh={2}", ParentId, Content.Replace("\r\n", "\n"), modhash),
                "http://www.reddit.com/api/comment");
        }
    }
}
