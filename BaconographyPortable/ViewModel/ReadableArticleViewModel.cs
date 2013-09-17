using BaconographyPortable.Messages;
using BaconographyPortable.Services;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.ServiceLocation;
using NBoilerpipePortable.Extractors;
using NBoilerpipePortable.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel
{
    public class ReadableArticleParagraph
    {
        public string Text { get; set; }
    }

    public class ReadableArticleImage
    {
        public string Url { get; set; }
    }

    public class ReadableArticleViewModel : ViewModelBase
    {
        public ReadableArticleViewModel()
        {
            _launchBrowser = new RelayCommand(LaunchBrowserImpl);
        }
        public static Task<ReadableArticleViewModel> LoadAtLeastOne(ISimpleHttpService httpService, string url, string linkId)
        {
            TaskCompletionSource<ReadableArticleViewModel> result = new TaskCompletionSource<ReadableArticleViewModel>();
            var articleViewModel = new ReadableArticleViewModel { ArticleUrl = url, ArticleParts = new ObservableCollection<object>(), LinkId = linkId, ContentIsFocused = true };
            LoadOneImpl(httpService, url, articleViewModel.ArticleParts).ContinueWith(async (task) =>
                {
                    try
                    {
                        if (task.IsCompleted)
                        {
                            Tuple<string, string> tpl = await task;
                            var nextPage = tpl.Item1;
                            articleViewModel.Title = tpl.Item2;
                            result.SetResult(articleViewModel);
                            if (!string.IsNullOrEmpty(nextPage))
                            {
                                var remainingParts = await Task.Run(() => LoadFullyImpl(httpService, nextPage));
                                foreach (var part in remainingParts.Item2)
                                {
                                    
                                    articleViewModel.ArticleParts.Add(part);
                                }
                            }
                        }
                        else if (task.Exception != null)
                            result.SetException(task.Exception);
                    }
                    catch (Exception ex)
                    {
                        result.SetException(ex);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            return result.Task;
            
        }

        public static async Task<ReadableArticleViewModel> LoadFully(ISimpleHttpService httpService, string url, string linkId)
        {
            var resultTpl = await LoadFullyImpl(httpService, url);
            return new ReadableArticleViewModel { LinkId = linkId, ArticleUrl = url, ArticleParts = new ObservableCollection<object>(resultTpl.Item2), Title = resultTpl.Item1 };
        }

        private static async Task<Tuple<string, string>> LoadOneImpl(ISimpleHttpService httpService, string url, IList<Object> target)
        {
            string domain = url;
            if(Uri.IsWellFormedUriString(url, UriKind.Absolute))
                domain = new Uri(url).Authority;
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true, Percentage = 0, Message = "loading from " + domain });
            var page = await httpService.UnAuthedGet(url);
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true, Percentage = 50, Message = "processing page from " + domain });
            string title;
            var pageBlocks = ArticleExtractor.INSTANCE.GetTextAndImageBlocks(page, new Uri(url), out title);
            foreach (var tpl in pageBlocks)
            {
                if (!string.IsNullOrEmpty(tpl.Item2))
                {
                    target.Add(new ReadableArticleImage { Url = tpl.Item2 });
                }

                StringBuilder articleContentsBuilder = new StringBuilder();
                foreach (var pp in tpl.Item1.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (target.Count > 200)
                        break;

                    articleContentsBuilder.AppendLine(pp);
                    if (articleContentsBuilder.Length > 1000)
                    {
                        target.Add(new ReadableArticleParagraph { Text = articleContentsBuilder.ToString() });
                        articleContentsBuilder.Clear();
                    }
                    
                }
                if (articleContentsBuilder.Length > 0)
                {
                    target.Add(new ReadableArticleParagraph { Text = articleContentsBuilder.ToString() + "\n\n"});
                }
            }
            var nextPageUrl = MultiPageUtils.FindNextPageLink(SgmlDomBuilder.GetBody(SgmlDomBuilder.BuildDocument(page)), url);
            Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
            return Tuple.Create(nextPageUrl, title);
        }

        private static async Task<Tuple<string, IEnumerable<object>>> LoadFullyImpl(ISimpleHttpService httpService, string url)
        {
            string nextUrl = url;
            string title = null;
            List<object> result = new List<object>();
            while (!string.IsNullOrEmpty(nextUrl))
            {
                var loadResult = await LoadOneImpl(httpService, nextUrl, result);
                if (title == null)
                    title = loadResult.Item2;

                nextUrl = loadResult.Item1;
            }
            return Tuple.Create<string, IEnumerable<object>>(title, result);
        }
        public string LinkId { get; set; }
        public string ArticleUrl { get; set; }
        public string ArticleDomain
        {
            get
            {
                if (Uri.IsWellFormedUriString(ArticleUrl, UriKind.Absolute))
                {
                    var uri = new Uri(ArticleUrl);
                    return uri.Authority;
                }
                else
                    return ArticleUrl;
            }
        }
        public string Title { get; set; }
        public bool WasStreamed { get; set; }
        public ObservableCollection<Object> ArticleParts { get; set; }

        private RelayCommand _launchBrowser;
        public RelayCommand LaunchBrowser
        {
            get
            {
                return _launchBrowser;
            }
        }

        private void LaunchBrowserImpl()
        {
            ServiceLocator.Current.GetInstance<INavigationService>().NavigateToExternalUri(new Uri(ArticleUrl));
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

        private bool _contentIsFocused = false;
        public bool ContentIsFocused
        {
            get
            {
                return _contentIsFocused;
            }
            set
            {
                if (_contentIsFocused != value)
                {
                    _contentIsFocused = value;
                    RaisePropertyChanged("ContentIsChanged");
                }
            }
        }

        public RelayCommand NavigateToComments 
        { 
            get 
            {
                if (_navigateToComments == null)
                    _navigateToComments = new RelayCommand(NavigateToCommentsImpl);
                return _navigateToComments; 
            } 
        }
        RelayCommand _navigateToComments;
        private void NavigateToCommentsImpl()
        {
            ParentLink.NavigateToComments.Execute(ParentLink);
        }
    }
}
