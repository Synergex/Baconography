using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Baconography.Messages;
using Baconography.RedditAPI;
using Baconography.RedditAPI.Things;
using Baconography.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Baconography.OfflineStore;

namespace Baconography.ViewModel
{
    public class LinkViewModel : ViewModelBase
    {
        TypedThing<Link> _linkThing;
        IRedditActionQueue _actionQueue;
        INavigationService _nav;

        public LinkViewModel(Thing linkThing, IRedditActionQueue actionQueue, INavigationService nav)
        {
            _linkThing = new TypedThing<Link>(linkThing);
            _actionQueue = actionQueue;
            _nav = nav;
        }

        VotableViewModel _votable;
        public VotableViewModel Votable
        {
            get
            {
                if (_votable == null)
                    _votable = new VotableViewModel(new TypedThing<IVotable>(_linkThing), _actionQueue);
                return _votable;
            }
        }

        public bool HasThumbnail
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Thumbnail);
            }
        }

        public string Thumbnail
        {
            get
            {
                return _linkThing.Data.Thumbnail;
            }
        }

        public DateTime CreatedUTC
        {
            get
            {
                return _linkThing.Data.CreatedUTC;
            }
        }

        //this should show only moderator info
        public Brush AuthorFlair
        {
            get
            {
                return new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            }
        }

        public string Author
        {
            get
            {
                return _linkThing.Data.Author;
            }
        }

        public string Subreddit
        {
            get
            {
                return _linkThing.Data.Subreddit;
            }
        }

        public string Title
        {
            get
            {
                return _linkThing.Data.Title;
            }
        }

        public int CommentCount
        {
            get
            {
                return _linkThing.Data.CommentCount;
            }
        }

        public bool IsSelfPost
        {
            get
            {
                return _linkThing.Data.IsSelf;
            }
        }

        RelayCommand _navigateToComments;
        public RelayCommand NavigateToComments
        {
            get
            {
                if (_navigateToComments == null)
                    _navigateToComments = new RelayCommand(() =>
                        {
                            _nav.Navigate<Baconography.View.CommentsView>(new SelectCommentTree { LinkThing = _linkThing });
                        });

                return _navigateToComments;
            }
        }

        RelayCommand _gotoLink;
        public RelayCommand GotoLink
        {
            get
            {
                if (_gotoLink == null)
                {
                    _gotoLink = new RelayCommand(async () =>
                        {
                            var imageResults = await Images.GetImagesFromUrl(_linkThing.Data.Title, _linkThing.Data.Url);
                            if (imageResults != null && imageResults.Count() > 0)
                            {
                                _nav.Navigate<Baconography.View.LinkedPictureView>(imageResults);
                            }
                            else
                            {
                                //its not an image url we can understand so whatever it is just show it in the browser
                                _nav.Navigate<Baconography.View.LinkedWebView>(new NavigateToUrlMessage { TargetUrl = _linkThing.Data.Url, Title = _linkThing.Data.Title });
                            }
                        });
                }
                return _gotoLink;
            }
        }
    }
}
