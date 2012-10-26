using GalaSoft.MvvmLight;
using Baconography.Messages;
using Baconography.RedditAPI;
using Baconography.RedditAPI.Things;
using Baconography.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.ViewModel
{
    public class UserDetailsViewModel : ViewModelBase
    {
        IUsersService _userService;
        IRedditActionQueue _actionQueue;
        INavigationService _nav;
        TypedThing<Account> _accountThing;
        /// <summary>
        /// Initializes a new instance of the UserDetailsViewModel class.
        /// </summary>
        public UserDetailsViewModel(IUsersService userService, IRedditActionQueue actionQueue, INavigationService nav)
        {
            _userService = userService;
            _actionQueue = actionQueue;
            _nav = nav;

            MessengerInstance.Register<SelectUserAccount>(this, (msg) => LoadAccount(msg.Account));
        }

        private void LoadAccount(TypedThing<Account> accountThing)
        {
            _accountThing = accountThing;

            Things = new ThingViewModelCollection
            {
                ActionQueue = _actionQueue,
                TargetListing = new Listing { Data = new ListingData { Children = new List<Thing>() } },
                BaseListingUrl = "http://reddit.com/user/" + accountThing.Data.Name,
                UserService = _userService,
                NavigationService = _nav
            };
        }

        public string UserName
        {
            get
            {
                return _accountThing.Data.Name;
            }
        }

        public int LinkKarma
        {
            get
            {
                return _accountThing.Data.LinkKarma;
            }
        }

        public int CommentKarma
        {
            get
            {
                return _accountThing.Data.CommentKarma;
            }
        }

        public DateTime Age
        {
            get
            {
                return _accountThing.Data.CreatedUTC;
            }
        }

        public ThingViewModelCollection Things { get; private set; }

        public ViewModelBase SelectedThing
        {
            get
            {
                return null;
            }
            set
            {
                if (value is LinkViewModel)
                    ((LinkViewModel)value).NavigateToComments.Execute(null);
                else if (value is CommentViewModel)
                    ((CommentViewModel)value).GotoContext.Execute(null);
            }
        }
    }
}
