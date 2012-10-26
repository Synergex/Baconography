using GalaSoft.MvvmLight;
using Baconography.RedditAPI;
using Baconography.RedditAPI.Actions;
using Baconography.RedditAPI.Things;
using Baconography.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.ViewModel
{
    public class SubredditViewModel : ViewModelBase
    {
        TypedThing<Subreddit> _subredditThing;
        IRedditActionQueue _actionQueue;
        INavigationService _nav;
        User _currentUser;

        public SubredditViewModel(Thing subredditThing, IRedditActionQueue actionQueue, INavigationService nav, User currentUser, bool subscribed)
        {
            _subredditThing = new TypedThing<Subreddit>(subredditThing);
            _actionQueue = actionQueue;
            _nav = nav;
            _currentUser = currentUser;
            _subscribed = subscribed;
        }

        public bool Over18
        {
            get
            {
                return _subredditThing.Data.Over18;
            }
        }

        public long Subscribers
        {
            get
            {
                return _subredditThing.Data.Subscribers;
            }
        }

        public DateTime CreatedUTC
        {
            get
            {
                return _subredditThing.Data.CreatedUTC;
            }
        }

        public string Url
        {
            get
            {
                return _subredditThing.Data.Url;
            }
        }

        public TypedThing<Subreddit> Thing
        {
            get
            {
                return _subredditThing;
            }
        }

        public string DisplayName
        {
            get
            {
                return _subredditThing.Data.DisplayName;    
            }
        }

        public string PublicDescription
        {
            get
            {
                return _subredditThing.Data.PublicDescription;
            }
        }

        public string HeaderImage
        {
            get
            {
                return _subredditThing.Data.HeaderImage;
            }
        }

        public int HeaderImageHeight
        {
            get
            {
                if (_subredditThing.Data.HeaderSize == null)
                    return 50;
                else
                    return _subredditThing.Data.HeaderSize[0];
            }
        }

        public int HeaderImageWidth
        {
            get
            {
                if (_subredditThing.Data.HeaderSize == null)
                    return 128;
                else
                    return _subredditThing.Data.HeaderSize[0];
            }
        }

        bool _subscribed;
        public bool Subscribed
        {
            get
            {
                return _subscribed;
            }
            set
            {
                _subscribed = value;
                _actionQueue.AddAction(new AddSubredditSubscription { Subreddit = _subredditThing.Data.Name, Unsub = !value });
                RaisePropertyChanged("Subscribed");
            }
        }


    }
}
