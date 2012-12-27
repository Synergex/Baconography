using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel.Collections
{
    public class UserActivityViewModelCollection : ThingViewModelCollection
    {
        public UserActivityViewModelCollection(IBaconProvider baconProvider, string username)
            : base(baconProvider,
                new BaconographyPortable.Model.Reddit.ListingHelpers.UserActivity(baconProvider, username),
                new BaconographyPortable.Model.KitaroDB.ListingHelpers.UserActivity()) { } 
    }
}
