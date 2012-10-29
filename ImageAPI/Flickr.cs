using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Baconography.ImageAPI
{
    class Flickr
    {
        //Transliterated from Reddit Enhancement Suite https://github.com/honestbleeps/Reddit-Enhancement-Suite/blob/master/lib/reddit_enhancement_suite.user.js
        private static Regex hashRe = new Regex(@"^http:\/\/(?:\w+).?flickr.com\/(?:.*)\/([\d]{10})\/?(?:.*)?$");

        internal static async Task<IEnumerable<Tuple<string, string>>> GetImagesFromUri(string title, Uri uri)
        {
            var href = uri.OriginalString.Split('?')[0];
            var groups = hashRe.Match(href).Groups;

            if (groups.Count > 2 && string.IsNullOrWhiteSpace(groups[2].Value))
            {
                var photoID = groups[1].Value;
                var apiURL = string.Format("http://api.flickr.com/services/rest/?method=flickr.photos.getSizes&api_key=81afa34d85f53254ff12a8cb73cba64d&photo_id={0}&format=json&nojsoncallback=1", photoID);

                var getClient = new HttpClient();
                var jsonResult = await getClient.GetStringAsync(apiURL);
                dynamic result = JsonConvert.DeserializeObject(jsonResult);

                var biggest = 0;
                var source = "";
                foreach (var sz in result.sizes.size)
                {
                    if ((int)sz.width > biggest)
                    {
                        biggest = (int)sz.width;
                        source = sz.source;
                    }
                }
                return new Tuple<string, string>[] { Tuple.Create(title, source) };
            }
            else
                return Enumerable.Empty<Tuple<string, string>>();
        }
    }
}
