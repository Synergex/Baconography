using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.RedditAPI.Actions
{
    class AddMessage : IRedditAction
    {
        public string Recipient { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }

        public async void Run(User loggedInUser)
        {
            var modhash = (string)loggedInUser.Me.ModHash;
            await loggedInUser.SendPost(string.Format("id={0}&uh={1}&to={2}&text={3}&subject={4}&thing-id={5}&renderstyle={6}", 
                "#compose-message", modhash, Recipient, Message, Subject, "", "html"),
                "http://www.reddit.com/api/compose");
            
        }
    }
}
