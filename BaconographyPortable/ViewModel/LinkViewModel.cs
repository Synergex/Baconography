using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
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
    public class LinkViewModel : ViewModelBase
    {
        TypedThing<Link> _linkThing;
        IRedditService _redditService;
        INavigationService _navigationService;
        IImagesService _imagesService;
        IDynamicViewLocator _dynamicViewLocator;
        IBaconProvider _baconProvider;

        public LinkViewModel(Thing linkThing, IBaconProvider baconProvider)
        {
            _linkThing = new TypedThing<Link>(linkThing);
            _baconProvider = baconProvider;
            _redditService = _baconProvider.GetService<IRedditService>();
            _navigationService = _baconProvider.GetService<INavigationService>();
            _imagesService = _baconProvider.GetService<IImagesService>();
            _dynamicViewLocator = _baconProvider.GetService<IDynamicViewLocator>();
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

        public RelayCommand<LinkViewModel> NavigateToComments { get { return _navigateToComments; } }
        public RelayCommand<LinkViewModel> GotoLink { get { return _gotoLink; } }

        static RelayCommand<LinkViewModel> _navigateToComments = new RelayCommand<LinkViewModel>(NavigateToCommentsImpl);
        static RelayCommand<LinkViewModel> _gotoLink = new RelayCommand<LinkViewModel>(GotoLinkImpl);

        private static void NavigateToCommentsImpl(LinkViewModel vm)
        {
            vm._navigationService.Navigate(vm._dynamicViewLocator.CommentsView, new SelectCommentTreeMessage { LinkThing = vm._linkThing });
        }
        
        private static async void GotoLinkImpl(LinkViewModel vm)
        {
            var imageResults = await vm._imagesService.GetImagesFromUrl(vm._linkThing.Data.Title, vm._linkThing.Data.Url);
            if (imageResults != null && imageResults.Count() > 0)
            {
                vm._navigationService.Navigate(vm._dynamicViewLocator.LinkedPictureView, imageResults);
            }
            else
            {
                //its not an image url we can understand so whatever it is just show it in the browser
                vm._navigationService.Navigate(vm._dynamicViewLocator.LinkedWebView, new NavigateToUrlMessage { TargetUrl = vm._linkThing.Data.Url, Title = vm._linkThing.Data.Title });
            }
        }
    }
}
