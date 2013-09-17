using BaconographyPortable.Common;
using BaconographyPortable.Messages;
using BaconographyPortable.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel
{
    public class LinkedPictureViewModel : ViewModelBase
    {
        public class LinkedPicture : ViewModelBase
        {
			private object _imageSource;
            public object ImageSource
			{
				get
				{
					return _imageSource;
				}
				set
				{
					_imageSource = value;
					RaisePropertyChanged("ImageSource");
				}
			}
            public string Url { get; set; }
            public string Title { get; set; }
            public bool IsAlbum { get; set; }
            public int PositionInAlbum { get; set; }
            public int AlbumSize { get; set; }
            public bool IsGif { get; set; }
            public bool HasExtendableTitle
            {
                get
                {
                    return Title.Length > 40 || Title.Contains("\r") || Title.Contains("\n");
                }
            }
        }

        public string LinkId { get; set; }

        public IEnumerable<LinkedPicture> _pictures;
        public IEnumerable<LinkedPicture> Pictures
        {
            get
            {
                return _pictures;
            }
            set
            {
                if (value != null)
                {
                    var refiedValue = value.ToList();
                    if (refiedValue.Count > 1)
                    {
                        int i = 1;
                        foreach (var picture in refiedValue)
                        {
                            picture.IsAlbum = true;
                            picture.PositionInAlbum = i++;
                            picture.AlbumSize = refiedValue.Count;
                        }
                    }
                    else
                    {
                        foreach (var picture in refiedValue)
                        {
                            picture.IsAlbum = false;
                            picture.PositionInAlbum = 1;
                            picture.AlbumSize = 1;
                        }
                    }
                    _pictures = refiedValue;
                }
                else
                    _pictures = null;
            }
        }
        public string ImageTitle
        {
            get
            {
                var firstPicture = Pictures.FirstOrDefault();
                if (firstPicture != null)
                    return firstPicture.Title;
                else
                    return "";
            }
        }

        public string LinkTitle
        {
            get;
            set;
        }

        public bool IsAlbum
        {
            get
            {
                return Pictures != null && Pictures.Count() > 1;
            }
        }

        LinkViewModel _parentLink;
        public LinkViewModel ParentLink
        {
            get
            {
                if (_parentLink == null)
                {
                    if (string.IsNullOrWhiteSpace(LinkId))
                        return null;

                    var viewModelContextService = ServiceLocator.Current.GetInstance<IViewModelContextService>();
                    var firstRedditViewModel = viewModelContextService.ContextStack.FirstOrDefault(context => context is RedditViewModel) as RedditViewModel;
                    if (firstRedditViewModel != null)
                    {
                        for (int i = 0; i < firstRedditViewModel.Links.Count; i++)
                        {
                            var linkViewModel = firstRedditViewModel.Links[i] as LinkViewModel;
                            if (linkViewModel != null)
                            {
                                if (linkViewModel.LinkThing.Data.Id == LinkId)
                                {
                                    _parentLink = linkViewModel;
                                    break;
                                }
                            }
                        }
                    }
                }

                return _parentLink;
            }
        }

        public bool HasContext
        {
            get
            {
                return ParentLink != null;
            }
        }

        public int CommentCount
        {
            get
            {
                if (HasContext)
                    return ParentLink.LinkThing.Data.CommentCount;
                return 0;
            }
        }

        public VotableViewModel Votable
        {
            get
            {
                if (ParentLink != null)
                    return ParentLink.Votable;
                return null;
            }
        }

        public RelayCommand<LinkedPictureViewModel> NavigateToComments { get { return _navigateToComments; } }
        static RelayCommand<LinkedPictureViewModel> _navigateToComments = new RelayCommand<LinkedPictureViewModel>(NavigateToCommentsImpl);
        private static void NavigateToCommentsImpl(LinkedPictureViewModel vm)
        {
            vm.ParentLink.NavigateToComments.Execute(vm.ParentLink);
        }


        
    }
}
