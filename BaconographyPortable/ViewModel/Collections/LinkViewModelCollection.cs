using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel.Collections
{
    public class LinkViewModelCollection : ThingViewModelCollection
    {
        public LinkViewModelCollection(IBaconProvider baconProvider, string subreddit, string subredditId = null)
            : base(baconProvider,
                new BaconographyPortable.Model.Reddit.ListingHelpers.SubredditLinks(baconProvider, subreddit, subredditId),
                new BaconographyPortable.Model.KitaroDB.ListingHelpers.SubredditLinks(baconProvider, subreddit, subredditId)) { }



        private HashSet<string> _itemIds = new HashSet<string>();

        public override async Task<int> LoadMoreItemsAsync(uint count)
        {
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });

            int addCounter = 0;

            if (_initialLoaded)
            {
                foreach (var item in await LoadAdditional(_state))
                {
                    if (item is LinkViewModel)
                    {
                        if (_itemIds.Contains(((LinkViewModel)item).Id))
                            continue;
                        else
                            _itemIds.Add(((LinkViewModel)item).Id);

                    }
                    addCounter++;
                    Add(item);
                }
            }
            else
            {
                _initialLoaded = true;
                foreach (var item in await InitialLoad(_state))
                {
                    if (item is LinkViewModel)
                    {
                        if (_itemIds.Contains(((LinkViewModel)item).Id))
                            continue;
                        else
                            _itemIds.Add(((LinkViewModel)item).Id);

                    }

                    addCounter++;
                    Add(item);
                }
            }

            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
            return addCounter;
        }
    }
}
