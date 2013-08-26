using BaconographyPortable.Model;
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
        Task MaybeCreateTile(Tuple<string, string, TypedThing<Link>> thingTpl);
        Task CreateSecondaryTileForSubreddit(TypedThing<Subreddit> subreddit);
        bool TileExists(string name);
        void RemoveSecondaryTile(string name);
        void SetCount(int count);
        void SetMessageRead(string id);
        IEnumerable<string> GetMessagesMarkedRead();
        TaskSettings? LoadTaskSettings();
        void StoreTaskSettings(Func<TaskSettings?, TaskSettings> getSettings, bool atomic);
    }
}
