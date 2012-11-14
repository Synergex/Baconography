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
    }
}
