using BaconographyPortable.Model.Reddit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Services
{
    public interface ILiveTileService
    {
        void MaybeCreateTile(Thing thing);
        Task CreateSecondaryTileForSubreddit(TypedThing<Subreddit> subreddit);
    }
}
