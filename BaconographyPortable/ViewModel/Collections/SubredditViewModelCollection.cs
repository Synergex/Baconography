using BaconographyPortable.Common;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel.Collections
{
    public class SubredditViewModelCollection : ThingViewModelCollection
    {

        public SubredditViewModelCollection(IBaconProvider baconProvider)
            : base(baconProvider,
                new BaconographyPortable.Model.Reddit.ListingHelpers.SubredditInfo(baconProvider),
                new BaconographyPortable.Model.KitaroDB.ListingHelpers.SubredditInfo(baconProvider)) { }
    }
}
