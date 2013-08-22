using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
using BaconographyPortable.ViewModel.Collections;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyWP8Core.ViewModel
{
    public class SubredditPickerViewModel : ViewModelBase
    {

        IRedditService _redditService;
        INavigationService _navigationService;
        IUserService _userService;
        IDynamicViewLocator _dynamicViewLocator;
        IBaconProvider _baconProvider;
        ISystemServices _systemServices;

        public SubredditPickerViewModel(IBaconProvider baconProvider)
        {
            _baconProvider = baconProvider;
            _redditService = _baconProvider.GetService<IRedditService>();
            _navigationService = _baconProvider.GetService<INavigationService>();
            _userService = _baconProvider.GetService<IUserService>();
            _dynamicViewLocator = _baconProvider.GetService<IDynamicViewLocator>();
            _systemServices = _baconProvider.GetService<ISystemServices>();
            Subreddits = new BindingShellViewModelCollection(new SubredditViewModelCollection(_baconProvider));
            SelectedSubreddits = new ObservableCollection<TypedSubreddit>();
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
                bool wasChanged = _text != value;
                if (wasChanged)
                {
                    _text = value;
                    RaisePropertyChanged("Text");

                    if (_text.Length < 3)
                    {
                        Subreddits.RevertToDefault();
                        RevokeQueryTimer();
                    }
                    else
                    {
                        RestartQueryTimer();
                    }
                }
            }
        }
        Object _queryTimer;
        void RevokeQueryTimer()
        {
            if (_queryTimer != null)
            {
                _systemServices.StopTimer(_queryTimer);
                _queryTimer = null;
            }
        }

        void RestartQueryTimer()
        {
            // Start or reset a pending query
            if (_queryTimer == null)
            {
                _queryTimer = _systemServices.StartTimer(queryTimer_Tick, new TimeSpan(0, 0, 1), true);
            }
            else
            {
                _systemServices.StopTimer(_queryTimer);
                _systemServices.RestartTimer(_queryTimer);
            }
        }

        void queryTimer_Tick(object sender, object timer)
        {
            // Stop the timer so it doesn't fire again unless rescheduled
            RevokeQueryTimer();
            if (Subreddits != null)
            {
                if (!(_text != null && _text.Contains("/")))
                    Subreddits.UpdateRealItems(new SearchResultsViewModelCollection(_baconProvider, _text, true));
            }
        }

        public void SetSubredditList(string subredditString)
        {
            _selectedSubreddits.Clear();
            Text = "";
            var redditsList = subredditString.Substring(subredditString.LastIndexOf('/') + 1).Split('+').ToList();
            foreach (var item in redditsList)
                AddSubreddit(item);
        }

        public async void AddSubreddit(string name)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(name))
                    return;

                var subreddit = await _redditService.GetSubreddit(name);
                if (subreddit != null)
                    _selectedSubreddits.Add(new TypedSubreddit(subreddit));
            }
            catch
            {

            }
        }

        public string GetSubredditString()
        {
            if (_selectedSubreddits.Count == 0)
                return "/";

            StringBuilder redditString = new StringBuilder("/r/");
            foreach (var item in _selectedSubreddits)
            {
                redditString.Append(item.DisplayName);
                redditString.Append('+');
            }
            return redditString.ToString().TrimEnd(new char[] { '+' });
        }

        public ObservableCollection<TypedSubreddit> _selectedSubreddits;
        public ObservableCollection<TypedSubreddit> SelectedSubreddits
        {
            get
            {
                return _selectedSubreddits;
            }
            set
            {
                _selectedSubreddits = value;
                RaisePropertyChanged("SelectedSubreddits");
            }
        }

        public BindingShellViewModelCollection Subreddits { get; set; }

    }

    public class TypedSubreddit
    {
        TypedThing<Subreddit> _thing;

        public TypedSubreddit(TypedThing<Subreddit> thing)
        {
            _thing = thing;
        }

        public string DisplayName
        {
            get
            {
                return _thing.Data.DisplayName;
            }
        }

        public string Name
        {
            get
            {
                return _thing.Data.Name;
            }
        }
    }
}
