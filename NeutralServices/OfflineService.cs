using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Baconography.NeutralServices
{
    class OfflineService : IOfflineService
    {
        public Task Clear()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Task<Tuple<string, byte[]>>>> GetImages(string uri)
        {
            throw new NotImplementedException();
        }

        public Task StoreComment(Thing comment)
        {
            throw new NotImplementedException();
        }

        public Task StoreComments(Listing listing)
        {
            throw new NotImplementedException();
        }

        public Task<Listing> GetTopLevelComments(string subredditId, string linkId, int count)
        {
            throw new NotImplementedException();
        }

        public Task<Listing> GetMoreComments(string after, int count)
        {
            throw new NotImplementedException();
        }

        public Task StoreLink(Thing link)
        {
            throw new NotImplementedException();
        }

        public Task StoreLinks(Listing listing)
        {
            throw new NotImplementedException();
        }

        public Task<Listing> LinksForSubreddit(string subredditName, string after)
        {
            throw new NotImplementedException();
        }

        public Task<Listing> AllLinks(string after)
        {
            throw new NotImplementedException();
        }

        public Task StoreOrderedThings(string key, IEnumerable<Thing> things)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Thing>> RetrieveOrderedThings(string key)
        {
            throw new NotImplementedException();
        }

        public Task StoreOrderedThings(IListingProvider listingProvider)
        {
            throw new NotImplementedException();
        }
    }

}