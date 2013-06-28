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

        public Type LinkedVideoView
        {
            get { return typeof(LinkedVideoView); }
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

        public Type AboutUserView
        {
            get { return typeof(AboutUserView); }
        }

		public Type MainView
		{
			get { return RedditView; }
		}

        public Type MessagesView
        {
            get { throw new NotImplementedException(); }
        }

        public Type ComposeView
        {
            get { throw new NotImplementedException(); }
        }
    }
}
