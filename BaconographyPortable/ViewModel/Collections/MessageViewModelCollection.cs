using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel.Collections
{
    public class MessageViewModelCollection : ThingViewModelCollection
    {
        public MessageViewModelCollection(IBaconProvider baconProvider) :
            base(baconProvider,
                new BaconographyPortable.Model.Reddit.ListingHelpers.UserMessages(baconProvider),
                new BaconographyPortable.Model.KitaroDB.ListingHelpers.UserMessages(baconProvider)) { }


    }
}
