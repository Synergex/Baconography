using BaconographyWP8.PlatformServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyWP8BackgroundTask
{
    class IntensiveTask
    {
        public static async Task Run(Action notifyWhenComplete)
        {
            //we want to download new images for the lock/tiles
            //we want to download image api/images/links/comments from the users pinned reddits
            //so when they wake up in the morning everything is fast
            //this task only runs when we're on wifi so there shouldnt be any concerns about bandwidth
            try
            {
                var baconProvider = new BaconProvider(new Tuple<Type, Object>[0]);

                await baconProvider.Initialize(null);



            }
            catch { }
            finally
            {
                notifyWhenComplete();
            }
        }
    }
}
