using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using BaconographyPortable.ViewModel.Collections;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel
{
    public class AboutUserViewModel : ViewModelBase
    {
        IBaconProvider _baconProvider;
        IUserService _userService;
        IRedditService _redditService;
        INavigationService _navigationService;
        IDynamicViewLocator _dynamicViewLocator;
        TypedThing<Account> _accountThing;

        public AboutUserViewModel(IBaconProvider baconProvider)
        {
            _baconProvider = baconProvider;
            _userService = baconProvider.GetService<IUserService>();
            _redditService = baconProvider.GetService<IRedditService>();;
            _navigationService = baconProvider.GetService<INavigationService>();
            _dynamicViewLocator = baconProvider.GetService<IDynamicViewLocator>();

            MessengerInstance.Register<SelectUserAccountMessage>(this, (msg) => LoadAccount(msg.Account));
        }

        private void LoadAccount(TypedThing<Account> accountThing)
        {
            _accountThing = accountThing;
            Things = new UserActivityViewModelCollection(_baconProvider, _accountThing.Data.Name);
        }

        public string UserName
        {
            get
            {
                return "/u/" + _accountThing.Data.Name;
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
                    ((LinkViewModel)value).GotoLink.Execute(value);
                else if (value is CommentViewModel)
                    ((CommentViewModel)value).GotoContext.Execute(value);
            }
        }
    }
}
