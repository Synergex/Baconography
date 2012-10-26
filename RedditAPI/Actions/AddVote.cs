using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.RedditAPI.Actions
{
    class AddVote : IRedditAction
    {
        public string PostId { get; set; }
        public int Direction { get; set; }

        public async void Run(User loggedInUser)
        {
            var modhash = (string)loggedInUser.Me.ModHash;


            var arguments = new Dictionary<string, string>
            {
                {"id", PostId},
                {"dir", Direction.ToString()},
                {"uh", modhash}
            };

            var result = await loggedInUser.SendPost(new FormUrlEncodedContent(arguments), "http://www.reddit.com/api/vote");
        }
    }
}
