using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel.Collections
{
    public class CommentReplyViewModelCollection : ThingViewModelCollection
    {
        public CommentReplyViewModelCollection(IBaconProvider baconProvider, IEnumerable<Thing> initialThings, string subreddit, string targetName)
            : base(baconProvider,
                new BaconographyPortable.Model.Reddit.ListingHelpers.ReplyComments(baconProvider, initialThings, subreddit, targetName),
                new BaconographyPortable.Model.KitaroDB.ListingHelpers.ReplyComments(initialThings)) { }

    }
}
