using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Baconography.ImageAPI
{
    class Memecrunch
    {
        private static Regex hashRe = new Regex(@"^http:\/\/memecrunch.com\/meme\/([0-9A-Z]+)\/([\w\-]+)(\/image\.(png|jpg))?");

        internal static IEnumerable<Tuple<string, string>> GetImagesFromUri(string title, Uri uri)
        {
            var href = uri.OriginalString;
            var groups = hashRe.Match(href).Groups;

            if (groups != null)
                return new Tuple<string, string>[] { Tuple.Create(title, string.Format("http://memecrunch.com/meme/{0}/{1}/image.png", groups[1].Value, groups[2].Value ?? "null")) };

            else
                return Enumerable.Empty<Tuple<string, string>>();
        }
    }
}
