using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.RedditAPI.Things
{
    public interface IVotable : IThingData
    {
        int Ups { get; set; }
        int Downs { get; set; }
        Nullable<bool> Likes { get; set; }
        string Id { get; set; }
        string Name { get; set; }
    }
}
