using BaconographyWP8.PlatformServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Baconography.PlatformServices.ImageAPI
{
    class Imgur
    {
        //Transliterated from Reddit Enhancement Suite https://github.com/honestbleeps/Reddit-Enhancement-Suite/blob/master/lib/reddit_enhancement_suite.user.js
        private static Regex hashRe = new Regex(@"^https?:\/\/(?:[i.]|[edge.]|[www.])*imgur.com\/(?:r\/[\w]+\/)?([\w]{5,}(?:[&,][\w]{5,})?)(\.[\w]{3,4})?(?:#(\d*))?(?:\?(?:\d*))?$");
        private static Regex albumHashRe = new Regex(@"^https?:\/\/(?:i\.)?imgur.com\/a\/([\w]+)(\..+)?(?:\/)?(?:#\d*)?$");
        private static string apiPrefix = "http://api.imgur.com/2/";

        internal static async Task<IEnumerable<Tuple<string, string>>> GetImagesFromUri(string title, Uri uri)
        {
            var href = uri.OriginalString;
            var groups = hashRe.Match(href).Groups;
            GroupCollection albumGroups = null;

            if (groups.Count == 0 || (groups.Count > 0 && string.IsNullOrWhiteSpace(groups[0].Value)))
                albumGroups = albumHashRe.Match(href).Groups;

            if (groups.Count > 2 && string.IsNullOrWhiteSpace(groups[2].Value))
            {
                if (Regex.IsMatch(groups[1].Value, "[&,]"))
                {
                    var hashes = Regex.Split(groups[1].Value, "[&,]");
                    //Imgur doesn't really care about the extension and the browsers don't seem to either.
                    return hashes
                        .Select(hash => Tuple.Create(title, string.Format("http://i.imgur.com/{0}.jpg", hash)));

                }
                else
                {
                    //Imgur doesn't really care about the extension and the browsers don't seem to either.
                    return new Tuple<string, string>[] { Tuple.Create(title, string.Format("http://i.imgur.com/{0}.jpg", groups[1].Value)) };
                }
            }
            else if (albumGroups.Count > 2 && string.IsNullOrWhiteSpace(albumGroups[2].Value))
            {
                var apiURL = string.Format("{0}album/{1}.json", apiPrefix, albumGroups[1].Value);
                var request = HttpWebRequest.CreateHttp(apiURL);
                string jsonResult;
                using (var response = (await SimpleHttpService.GetResponseAsync(request)))
                {
					jsonResult = await Task<string>.Run(() =>
					{
						using (var sr = new StreamReader(response.GetResponseStream()))
						{
							return sr.ReadToEnd();
						}
					});
                }
                var result = JsonConvert.DeserializeObject(jsonResult) as JObject;
                return ((IEnumerable)((JObject)result.GetValue("album")).GetValue("images"))
                    .Cast<JObject>()
                    .Select(e => Tuple.Create((string)((JObject)e.GetValue("image")).GetValue("caption"), (string)((JObject)e.GetValue("links")).GetValue("original")));
            }
            else
                return Enumerable.Empty<Tuple<string, string>>();
        }
    }
}
