using BaconographyPortable.Services;
using GalaSoft.MvvmLight;
using NBoilerpipePortable.Extractors;
using NBoilerpipePortable.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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
        public static Task<ReadableArticleViewModel> LoadAtLeastOne(ISimpleHttpService httpService, string url)
        {
            TaskCompletionSource<ReadableArticleViewModel> result = new TaskCompletionSource<ReadableArticleViewModel>();
            var articleViewModel = new ReadableArticleViewModel { ArticleUrl = url, ArticleParts = new ObservableCollection<object>() };
            LoadOneImpl(httpService, url, articleViewModel.ArticleParts).ContinueWith(async (task) =>
                {
                    if(task.IsCompleted)
                    {
                        string nextPage = await task;
                        result.SetResult(articleViewModel);
                        if (string.IsNullOrEmpty(nextPage))
                        {
                            var remainingParts = await Task.Run(() => LoadFullyImpl(httpService, nextPage));
                            foreach (var part in remainingParts)
                            {
                                articleViewModel.ArticleParts.Add(part);
                            }
                        }
                    }
                });
            return result.Task;
            
        }

        public static async Task<ReadableArticleViewModel> LoadFully(ISimpleHttpService httpService, string url)
        {
            return new ReadableArticleViewModel { ArticleUrl = url, ArticleParts = new ObservableCollection<object>(await LoadFullyImpl(httpService, url)) };
        }

        private static async Task<string> LoadOneImpl(ISimpleHttpService httpService, string url, IList<Object> target)
        {
            var page = await httpService.UnAuthedGet(url);
            var pageBlocks = ArticleExtractor.INSTANCE.GetTextAndImageBlocks(page, new Uri(url));
            foreach (var tpl in pageBlocks)
            {
                if (!string.IsNullOrEmpty(tpl.Item2))
                {
                    target.Add(new ReadableArticleImage { Url = tpl.Item2 });
                }
                target.Add(new ReadableArticleParagraph { Text = tpl.Item1 });
            }
            var nextPageUrl = MultiPageUtils.FindNextPageLink(SgmlDomBuilder.GetBody(SgmlDomBuilder.BuildDocument(page)), url);
            return nextPageUrl;
        }

        private static async Task<IEnumerable<object>> LoadFullyImpl(ISimpleHttpService httpService, string url)
        {
            string nextUrl = url;
            List<object> result = new List<object>();
            while (!string.IsNullOrEmpty(nextUrl))
            {
                await LoadOneImpl(httpService, nextUrl, result); 
            }
            return result;
        }
        public string ArticleUrl { get; set; }
        public ObservableCollection<Object> ArticleParts { get; set; }
    }
}
