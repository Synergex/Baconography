using BaconographyPortable.Common;
using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel
{
    public class LinkViewModel : ViewModelBase
    {
        TypedThing<Link> _linkThing;
        IRedditService _redditService;
        INavigationService _navigationService;
        IImagesService _imagesService;
        IDynamicViewLocator _dynamicViewLocator;
        IBaconProvider _baconProvider;
        bool _isPreviewShown;

        public LinkViewModel(Thing linkThing, IBaconProvider baconProvider)
        {
            _linkThing = new TypedThing<Link>(linkThing);
            _baconProvider = baconProvider;
            _redditService = _baconProvider.GetService<IRedditService>();
            _navigationService = _baconProvider.GetService<INavigationService>();
            _imagesService = _baconProvider.GetService<IImagesService>();
            _dynamicViewLocator = _baconProvider.GetService<IDynamicViewLocator>();
            _isPreviewShown = false;
            ShowPreview = new RelayCommand(() => IsPreviewShown = !IsPreviewShown);
        }

        VotableViewModel _votable;
        public VotableViewModel Votable
        {
            get
            {
                if (_votable == null)
                    _votable = new VotableViewModel(_linkThing, _baconProvider);
                return _votable;
            }
        }

        public bool HasThumbnail
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Thumbnail) && Thumbnail != "self" && Thumbnail != "nsfw" && Thumbnail != "default";
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
        public AuthorFlairKind AuthorFlair
        {
            get
            {
                return AuthorFlairKind.None;
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

        public string Url
        {
            get
            {
                return _linkThing.Data.Url;
            }
        }

        public bool HasPreview
        {
            get
            {
                return _imagesService.MightHaveImagesFromUrl(Url);
            }
        }

        public bool IsPreviewShown
        {
            get
            {
                return _isPreviewShown;
            }
            set
            {
                _isPreviewShown = value;
                RaisePropertyChanged("IsPreviewShown");
                RaisePropertyChanged("PreviewPack");
            }
        }

        public Tuple<bool, string> PreviewPack
        {
            get
            {
                return Tuple.Create(IsPreviewShown, Url);
            }
        }

        public RelayCommand<LinkViewModel> NavigateToComments { get { return _navigateToComments; } }
        public RelayCommand<LinkViewModel> GotoLink { get { return _gotoLink; } }

        static RelayCommand<LinkViewModel> _navigateToComments = new RelayCommand<LinkViewModel>(NavigateToCommentsImpl);
        static RelayCommand<LinkViewModel> _gotoLink = new RelayCommand<LinkViewModel>(GotoLinkImpl);

        public RelayCommand ShowPreview { get; set; }


        private static void NavigateToCommentsImpl(LinkViewModel vm)
        {
            vm._navigationService.Navigate(vm._dynamicViewLocator.CommentsView, new SelectCommentTreeMessage { LinkThing = vm._linkThing });
        }

        private static void GotoLinkImpl(LinkViewModel vm)
        {
            UtilityCommandImpl.GotoLinkImpl(vm.Url);
        }
    }
}
