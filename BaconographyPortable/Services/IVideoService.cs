using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Services
{
    public interface IVideoService
    {
        Task<IEnumerable<Dictionary<string, string>>> GetPlayableStreams(string originalUrl);
    }
}
