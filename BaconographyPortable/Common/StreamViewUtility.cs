using BaconographyPortable.Messages;
using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Common
{
    public class StreamViewUtility
    {
        public static void RepositionContextScroll(LinkViewModel parentLink)
        {
            var viewModelContextService = ServiceLocator.Current.GetInstance<IViewModelContextService>();
            var firstRedditViewModel = viewModelContextService.ContextStack.FirstOrDefault(context => context is RedditViewModel) as RedditViewModel;
            if (firstRedditViewModel != null)
            {
                firstRedditViewModel.TopVisibleLink = parentLink;
            }
        }

        public static async Task<ViewModelBase> Previous(LinkViewModel parentLink, ViewModelBase currentActual)
        {
            if (parentLink != null)
            {
                var viewModelContextService = ServiceLocator.Current.GetInstance<IViewModelContextService>();
                var firstRedditViewModel = viewModelContextService.ContextStack.FirstOrDefault(context => context is RedditViewModel) as RedditViewModel;
                if (firstRedditViewModel != null)
                {
                    RepositionContextScroll(parentLink);

                    var imagesService = ServiceLocator.Current.GetInstance<IImagesService>();
                    var offlineService = ServiceLocator.Current.GetInstance<IOfflineService>();
                    var settingsService = ServiceLocator.Current.GetInstance<ISettingsService>();
                    //need to go backwards in time, not paying attention to the unread rules
                    ViewModelBase stackPrevious = null;
                    var emptyForward = LinkHistory.EmptyForward;
                    if (settingsService.OnlyFlipViewUnread && (stackPrevious = LinkHistory.Backward()) != null)
                    {
                        if (emptyForward)
                            LinkHistory.Push(currentActual);

                        return stackPrevious;
                    }
                    else
                    {
                        var currentLinkPos = firstRedditViewModel.Links.IndexOf(parentLink);
                        var linksEnumerator = new NeverEndingRedditView(firstRedditViewModel, currentLinkPos, false);
                        return await MakeContextedTuple(imagesService, offlineService, settingsService, linksEnumerator);
                    }

                }
            }
            return null;
        }

        public static async Task<ViewModelBase> Next(LinkViewModel parentLink, ViewModelBase currentActual)
        {
            if (parentLink != null)
            {
                var viewModelContextService = ServiceLocator.Current.GetInstance<IViewModelContextService>();
                var firstRedditViewModel = viewModelContextService.ContextStack.FirstOrDefault(context => context is RedditViewModel) as RedditViewModel;
                if (firstRedditViewModel != null)
                {
                    RepositionContextScroll(parentLink);

                    var imagesService = ServiceLocator.Current.GetInstance<IImagesService>();
                    var offlineService = ServiceLocator.Current.GetInstance<IOfflineService>();
                    var settingsService = ServiceLocator.Current.GetInstance<ISettingsService>();
                    ViewModelBase stackNext = null;
                    if (settingsService.OnlyFlipViewUnread && (stackNext = LinkHistory.Forward()) != null)
                    {
                        return stackNext;
                    }
                    else
                    {
                        var currentLinkPos = firstRedditViewModel.Links.IndexOf(parentLink);
                        var linksEnumerator = new NeverEndingRedditView(firstRedditViewModel, currentLinkPos, true);
                        var result = await MakeContextedTuple(imagesService, offlineService, settingsService, linksEnumerator);
                        LinkHistory.Push(currentActual);
                        return result;
                    }
                }
            }
            return null;
        }

        private static async Task<ViewModelBase> MakeContextedTuple(IImagesService imagesService, IOfflineService offlineService, ISettingsService settingsService, NeverEndingRedditView linksEnumerator)
        {
            ViewModelBase vm;
            while ((vm = await linksEnumerator.Next()) != null)
            {
                if (vm is LinkViewModel && imagesService.MightHaveImagesFromUrl(((LinkViewModel)vm).Url) && (!settingsService.OnlyFlipViewUnread || !offlineService.HasHistory(((LinkViewModel)vm).Url)))
                {
                    var targetViewModel = vm as LinkViewModel;
                    var smartOfflineService = ServiceLocator.Current.GetInstance<ISmartOfflineService>();
                    smartOfflineService.NavigatedToOfflineableThing(targetViewModel.LinkThing, false);
                    Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });
                    await ServiceLocator.Current.GetInstance<IOfflineService>().StoreHistory(targetViewModel.Url);
                    var imageResults = await ServiceLocator.Current.GetInstance<IImagesService>().GetImagesFromUrl(targetViewModel.LinkThing == null ? "" : targetViewModel.LinkThing.Data.Title, targetViewModel.Url);
                    Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });

                    if (imageResults != null && imageResults.Count() > 0)
                    {
                        var imageTuple = new Tuple<string, IEnumerable<Tuple<string, string>>, string>(targetViewModel.LinkThing != null ? targetViewModel.LinkThing.Data.Title : "", imageResults, targetViewModel.LinkThing != null ? targetViewModel.LinkThing.Data.Id : "");
                        Messenger.Default.Send<LongNavigationMessage>(new LongNavigationMessage { Finished = true, TargetUrl = targetViewModel.Url });
                        return new LinkedPictureViewModel
                        {
                            LinkTitle = imageTuple.Item1.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&apos;", "'").Trim(),
                            LinkId = imageTuple.Item3,
                            Pictures = imageTuple.Item2.Select(tpl => new LinkedPictureViewModel.LinkedPicture
                            {
                                Title = tpl.Item1.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&apos;", "'").Trim(),
                                ImageSource = tpl.Item2,
                                Url = tpl.Item2
                            })
                        };
                    }
                }
                else if (vm is LinkViewModel && LinkGlyphUtility.GetLinkGlyph(vm) == LinkGlyphUtility.WebGlyph && settingsService.ApplyReadabliltyToLinks && (!settingsService.OnlyFlipViewUnread || !offlineService.HasHistory(((LinkViewModel)vm).Url)))
                {
                    var targetViewModel = vm as LinkViewModel;
                    var smartOfflineService = ServiceLocator.Current.GetInstance<ISmartOfflineService>();
                    smartOfflineService.NavigatedToOfflineableThing(targetViewModel.LinkThing, true);
                    await ServiceLocator.Current.GetInstance<IOfflineService>().StoreHistory(targetViewModel.Url);
                    var result = await ReadableArticleViewModel.LoadAtLeastOne(ServiceLocator.Current.GetInstance<ISimpleHttpService>(), targetViewModel.Url, targetViewModel.LinkThing.Data.Id);
                    return result;
                }
                else if (vm is LinkViewModel && LinkGlyphUtility.GetLinkGlyph(vm) == LinkGlyphUtility.CommentGlyph)
                {
                    //do something for self posts, dont load the comments just the text
                }
            }
            return null;
        }

        public static EndlessStack<ViewModelBase> LinkHistory = new EndlessStack<ViewModelBase>(50);
    }
}
