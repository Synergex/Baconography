using BaconographyPortable.Services;
using BaconographyW8.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyW8.PlatformServices
{
    class DynamicViewLocator : IDynamicViewLocator
    {
        public Type RedditView
        {
            get { return typeof(RedditView); }
        }

        public Type LinkedPictureView
        {
            get { return typeof(LinkedPictureView); }
        }

        public Type LinkedWebView
        {
            get { return typeof(LinkedWebView); }
        }

        public Type CommentsView
        {
            get { return typeof(CommentsView); }
        }

        public Type SearchResultsView
        {
            get { return typeof(SearchResultsView); }
        }

        public Type SubredditsView
        {
            get { return typeof(SubredditsView); }
        }

        public Type SearchQueryView
        {
            get { return typeof(SearchQueryView); }
        }

        public Type SubmitToSubredditView
        {
            get { throw new NotImplementedException(); }
        }
    }
}
