using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Services
{
    public interface ISimpleHttpService
    {
        Task<string> SendPost(string cookie, string data, string uri);
        Task<string> SendPost(string cookie, Dictionary<string, string> urlEncodedData, string uri);
        Task<string> SendGet(string cookie, string uri);
        Task<Tuple<string, Dictionary<string, string>>> SendPostForCookies(Dictionary<string, string> urlEncodedData, string uri);
        Task<string> UnAuthedGet(string uri);
    }
}
