using Newtonsoft.Json;
using Baconography.RedditAPI.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Baconography.RedditAPI.Actions
{
    class GetSubscribedSubreddits
    {
        public Nullable<int> Limit { get; set; }

        public async Task<Listing> Run(User loggedInUser)
        {
            int limit = Limit ?? 100;
            if (Limit == null && loggedInUser.Me.IsGold)
            {
                limit = 1500;
            }

            var targetUri = string.Format("http://www.reddit.com/reddits/mine.json?limit={0}", limit);

            //if we get back nothing here we need to replace the returned string with the default subreddit result
            try
            {
                var comments = await loggedInUser.SendGet(targetUri);

                if (comments == "\"{}\"")
                    return await Defaults();
                else
                    return JsonConvert.DeserializeObject<Listing>(comments);
            }
            catch (Exception)
            {
                User.ShowDisconnectedMessage();
            }

            //cant await in the catch, but we can fall through to it
            return await Defaults();
        }

        public static async Task<Listing> Defaults()
        {
            var defaultSubredditsFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/default_reddits.json"));
            var defaultSubredditsText = await Windows.Storage.FileIO.ReadTextAsync(defaultSubredditsFile);
            return JsonConvert.DeserializeObject<Listing>(defaultSubredditsText);
        }
    }
}
