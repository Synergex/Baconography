﻿using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel
{
    public class VotableViewModel : ViewModelBase
    {
        TypedThing<IVotable> _votableThing;
        IRedditService _redditService;

        public VotableViewModel(Thing votableThing, IBaconProvider baconProvider)
        {
            _votableThing = new TypedThing<IVotable>(votableThing);
            _redditService = baconProvider.GetService<IRedditService>();
        }

        public RelayCommand<VotableViewModel> ToggleUpvote { get { return _toggleUpvote; } }
        public RelayCommand<VotableViewModel> ToggleDownvote { get { return _toggleDownvote; } }

        public int TotalVotes
        {
            get
            {
                return (_votableThing.Data.Ups - _votableThing.Data.Downs) + (Like ? 1 : 0) + (Dislike ? -1 : 0);
            }
        }

        public bool Like
        {
            get
            {
                return _votableThing.Data.Likes ?? false;
            }
            set
            {
                var currentLike = Like;
                if (value)
                    _votableThing.Data.Likes = true;
                else
                    _votableThing.Data.Likes = null;


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
                return !(_votableThing.Data.Likes ?? true);
            }
            set
            {
                var currentDislike = Dislike;
                if (value)
                    _votableThing.Data.Likes = false;
                else
                    _votableThing.Data.Likes = null;

                if (currentDislike != Dislike)
                {
                    RaisePropertyChanged("Like");
                    RaisePropertyChanged("Dislike");
                    RaisePropertyChanged("TotalVotes");
                }
            }
        }


        static RelayCommand<VotableViewModel> _toggleUpvote = new RelayCommand<VotableViewModel>(ToggleUpvoteImpl);
        static RelayCommand<VotableViewModel> _toggleDownvote = new RelayCommand<VotableViewModel>(ToggleDownvoteImpl);

        private static void ToggleUpvoteImpl(VotableViewModel vm)
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

            vm._redditService.AddVote(vm._votableThing.Data.Name, voteDirection);

        }

        private static void ToggleDownvoteImpl(VotableViewModel vm)
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

            vm._redditService.AddVote(vm._votableThing.Data.Name, voteDirection);
        }


    }
}
