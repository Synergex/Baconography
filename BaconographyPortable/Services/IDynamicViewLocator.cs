using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Services
{
    public interface IDynamicViewLocator
    {
        Type RedditView { get; }
        Type LinkedPictureView { get; }
        Type LinkedWebView { get; }
        Type CommentsView { get; }
        Type SearchResultsView { get; }
        Type SubredditsView { get; }
        Type SearchQueryView { get; }
        Type SubmitToSubredditView { get; }
        Type AboutUserView { get; }
        Type LinkedVideoView { get; }
		Type MainView { get; }
        Type MessagesView { get; }
        Type ComposeView { get; }
        Type CaptchaView { get; }
    }
}
