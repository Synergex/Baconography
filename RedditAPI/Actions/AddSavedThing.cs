using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.RedditAPI.Actions
{
    class AddSavedThing : IRedditAction
    {
        public string Id { get; set; }

        public async void Run(User loggedInUser)
        {
            var modhash = (string)loggedInUser.Me.ModHash;
            var targetUri = "http://www.reddit.com/api/save";

            FormUrlEncodedContent content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "id", Id},
                { "uh", modhash}
            });

            await loggedInUser.SendPost(content, targetUri);
        }
    }
}
