using BaconographyPortable.Common;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Model.Reddit.ListingHelpers;
using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel.Collections
{
    public class CommentViewModelCollection : ThingViewModelCollection
    {
        public CommentViewModelCollection(IBaconProvider baconProvider, string permaLink, string subreddit, string targetName)
            : base(baconProvider, 
                new BaconographyPortable.Model.Reddit.ListingHelpers.PostComments(baconProvider, subreddit, permaLink, targetName),
                new BaconographyPortable.Model.KitaroDB.ListingHelpers.PostComments(baconProvider, subreddit, permaLink, targetName)) { }

    }
}
