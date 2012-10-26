using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Baconography.RedditAPI;
using Baconography.RedditAPI.Actions;
using Baconography.RedditAPI.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.ViewModel
{
    public class VotableViewModel : ViewModelBase
    {
        TypedThing<IVotable> _linkThing;
        IRedditActionQueue _actionQueue;

        public VotableViewModel(TypedThing<IVotable> linkThing, IRedditActionQueue actionQueue)
        {
            _linkThing = linkThing;
            _actionQueue = actionQueue;
        }

        RelayCommand _toggleUpvote;
        public RelayCommand ToggleUpvote
        {
            get
            {
                if (_toggleUpvote == null)
                    _toggleUpvote = new RelayCommand(() => ToggleUpvoteImpl(this, _actionQueue));
                return _toggleUpvote;
            }
        }

        RelayCommand _toggleDownvote;
        public RelayCommand ToggleDownvote
        {
            get
            {
                if (_toggleDownvote == null)
                    _toggleDownvote = new RelayCommand(() => ToggleDownvoteImpl(this, _actionQueue));
                return _toggleDownvote;
            }
        }

        public bool Like
        {
            get
            {
                return _linkThing.Data.Likes ?? false;
            }
            set
            {
                var currentLike = Like;
                if (value)
                    _linkThing.Data.Likes = true;
                else
                    _linkThing.Data.Likes = null;


                if (currentLike != Like)
                {
                    RaisePropertyChanged("Like");
                    RaisePropertyChanged("Dislike");
                    RaisePropertyChanged("TotalVotes");
                }
            }
        }

        public bool Dislike
        {
            get
            {
                return !(_linkThing.Data.Likes ?? true);
            }
            set
            {
                var currentDislike = Dislike;
                if (value)
                    _linkThing.Data.Likes = false;
                else
                    _linkThing.Data.Likes = null;

                if (currentDislike != Dislike)
                {
                    RaisePropertyChanged("Like");
                    RaisePropertyChanged("Dislike");
                    RaisePropertyChanged("TotalVotes");
                }
            }
        }

        public int TotalVotes
        {
            get
            {
                return (_linkThing.Data.Ups - _linkThing.Data.Downs) + (Like ? 1 : 0) + (Dislike ? -1 : 0);
            }
        }

        private static void ToggleUpvoteImpl(VotableViewModel vm, IRedditActionQueue actionQueue)
        {
            int voteDirection = 0;
            if (!vm.Like) //moved to neutral
            {
                voteDirection = 0;
            }
            else
            {
                voteDirection = 1;
            }

            if (actionQueue != null)
                actionQueue.AddAction(new AddVote { Direction = voteDirection, PostId = vm._linkThing.Data.Name });

        }

        private static void ToggleDownvoteImpl(VotableViewModel vm, IRedditActionQueue actionQueue)
        {
            int voteDirection = 0;
            if (!vm.Dislike) //moved to neutral
            {
                voteDirection = 0;
            }
            else
            {
                voteDirection = -1;
            }

            if (actionQueue != null)
                actionQueue.AddAction(new AddVote { Direction = voteDirection, PostId = vm._linkThing.Data.Name });
        }
    }

}
