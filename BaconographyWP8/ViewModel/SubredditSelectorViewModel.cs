using BaconographyPortable.Messages;
using BaconographyPortable.Services;
using BaconographyPortable.ViewModel.Collections;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel
{
    public class SubredditSelectorViewModel : ViewModelBase
    {
        IRedditService _redditService;
        INavigationService _navigationService;
        IUserService _userService;
        IDynamicViewLocator _dynamicViewLocator;
        IBaconProvider _baconProvider;

		public SubredditSelectorViewModel(IBaconProvider baconProvider)
        {
            _baconProvider = baconProvider;
            _redditService = _baconProvider.GetService<IRedditService>();
            _navigationService = _baconProvider.GetService<INavigationService>();
            _userService = _baconProvider.GetService<IUserService>();
            _dynamicViewLocator = _baconProvider.GetService<IDynamicViewLocator>();

            Subreddits = new SubredditViewModelCollection(_baconProvider);
        }

		private string _text;
		public string Text
		{
			get
			{
				return _text;
			}
			set
			{
				_text = value;
				RaisePropertyChanged("Text");
			}
		}

        public AboutSubredditViewModel SelectedSubreddit
        {
            get
            {
                return null;
            }
            set
            {
				var message = new SelectSubredditMessage { Subreddit = value.Thing };
				MessengerInstance.Send<SelectSubredditMessage>(message);
            }
        }


		public RelayCommand<SubredditSelectorViewModel> PinSubreddit { get { return _pinSubreddit; } }
		static RelayCommand<SubredditSelectorViewModel> _pinSubreddit = new RelayCommand<SubredditSelectorViewModel>(PinSubredditImpl);

		private async static void PinSubredditImpl(SubredditSelectorViewModel vm)
		{
			vm.DoPinSubreddit();
		}

		private async void DoPinSubreddit()
		{
			var subredditName = Text;
			if (String.IsNullOrEmpty(subredditName))
				return;

			if (subredditName.Contains('/') && subredditName != "/")
				subredditName = subredditName.Substring(subredditName.LastIndexOf('/') + 1);

			var _redditService = ServiceLocator.Current.GetInstance<IRedditService>();
			if (_redditService == null)
				return;

			var subreddit = await _redditService.GetSubreddit(subredditName);
			if (subreddit == null)
				return;

			MessengerInstance.Send<SelectSubredditMessage>(new SelectSubredditMessage { Subreddit = subreddit });
		}

        public SubredditViewModelCollection Subreddits { get; private set; }
    }
}
