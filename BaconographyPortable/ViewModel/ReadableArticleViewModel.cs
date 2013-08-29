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
                    else if(task.Exception != null)
                        result.SetException(task.Exception);
                });
            return result.Task;
            
        }

        public static async Task<ReadableArticleViewModel> LoadFully(ISimpleHttpService httpService, string url)
        {
            var resultTpl = await LoadFullyImpl(httpService, url);
            return new ReadableArticleViewModel { ArticleUrl = url, ArticleParts = new ObservableCollection<object>(resultTpl.Item2), Title = resultTpl.Item1 };
        }

        private static async Task<Tuple<string, string>> LoadOneImpl(ISimpleHttpService httpService, string url, IList<Object> target)
        {
            var page = await httpService.UnAuthedGet(url);
            string title;
            var pageBlocks = ArticleExtractor.INSTANCE.GetTextAndImageBlocks(page, new Uri(url), out title);
            foreach (var tpl in pageBlocks)
            {
                if (!string.IsNullOrEmpty(tpl.Item2))
                {
                    target.Add(new ReadableArticleImage { Url = tpl.Item2 });
                }

                foreach (var pp in tpl.Item1.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    target.Add(new ReadableArticleParagraph { Text = pp });
                }
            }
            var nextPageUrl = MultiPageUtils.FindNextPageLink(SgmlDomBuilder.GetBody(SgmlDomBuilder.BuildDocument(page)), url);
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
            }
            return Tuple.Create<string, IEnumerable<object>>(title, result);
        }
        public string ArticleUrl { get; set; }
        public string Title { get; set; }
        public ObservableCollection<Object> ArticleParts { get; set; }
    }
}
