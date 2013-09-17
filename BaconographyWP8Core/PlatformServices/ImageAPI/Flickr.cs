using BaconographyPortable.Services;
using BaconographyWP8.PlatformServices;
using Microsoft.Practices.ServiceLocation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Baconography.PlatformServices.ImageAPI
{
    class Flickr
    {
        //Transliterated from Reddit Enhancement Suite https://github.com/honestbleeps/Reddit-Enhancement-Suite/blob/master/lib/reddit_enhancement_suite.user.js
        private static Regex hashRe = new Regex(@"^http:\/\/(?:\w+)\.?flickr\.com\/(?:.*)\/([\d]{10})\/?(?:.*)?$");

        internal static bool IsAPI(Uri uri)
        {
            return hashRe.IsMatch(uri.OriginalString);
        }

        struct OEmbedResult
        {
            public string version;
	        public string type;
	        public int width;
	        public int height;
	        public string title;
	        public string url;
	        public string author_name;
	        public string author_url;
	        public string provider_name;
            public string provider_url;
        }

        internal static async Task<IEnumerable<Tuple<string, string>>> GetImagesFromUri(string title, Uri uri)
        {
            try
            {
                var href = uri.OriginalString;
                if (href.IndexOf("/sizes") == -1)
                {
                    var inPosition = href.IndexOf("/in/");
                    var inFragment = "";
                    if (inPosition != -1)
                    {
                        inFragment = href.Substring(inPosition);
                        href = href.Substring(0, inPosition);
                    }

                    href += "/sizes/c" + inFragment;
                }
                href = href.Replace("/lightbox", "");
                var resultJson = await ServiceLocator.Current.GetInstance<ISimpleHttpService>().UnAuthedGet("http://www.flickr.com/services/oembed/?format=json&url=" + HttpUtility.UrlEncode(href));
                var resultObject = JsonConvert.DeserializeObject<OEmbedResult>(resultJson);
                return new Tuple<string, string>[] { Tuple.Create(resultObject.author_name + " via " + resultObject.provider_name + " : " + resultObject.title, resultObject.url) };
            }
            catch
            {
                return Enumerable.Empty<Tuple<string, string>>();
            }
        }
    }
}
