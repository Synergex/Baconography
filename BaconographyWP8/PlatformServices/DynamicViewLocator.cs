using BaconographyPortable.Services;
using BaconographyWP8.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyWP8.PlatformServices
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
			get { return null; }
        }

        public Type CommentsView
        {
			get { return typeof(CommentsView); }
        }

        public Type SearchResultsView
        {
			get { throw new NotImplementedException(); }
        }

        public Type SubredditsView
        {
			get { throw new NotImplementedException(); }
        }

        public Type SearchQueryView
        {
			get { throw new NotImplementedException(); }
        }

        public Type SubmitToSubredditView
        {
            get { throw new NotImplementedException(); }
        }

        public Type AboutUserView
        {
			get { throw new NotImplementedException(); }
        }

		public Type LinkedVideoView
		{
			get { throw new NotImplementedException(); }
		}

		public Type MainView
		{
			get { return typeof(MainPage); }
		}
    }
}
