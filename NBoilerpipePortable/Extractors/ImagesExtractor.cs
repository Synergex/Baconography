using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBoilerpipePortable.Extractors
{
    public class ImagesExtractor
    {
        public struct ExtractedImage
        {
            public string src;
            public int? height;
            public int? width;
            public string alt;
        }

        private static int? GetNullableInt(string value)
        {
            int result;
            if (int.TryParse(value, out result))
                return result;
            else
                return null;
        }

        public static List<ExtractedImage> GetImages(string html)
        {
            var document = new HtmlAgilityPack.HtmlDocument();
            document.LoadHtml(html);
            return document.DocumentNode.Descendants("img")
                                            .Select(e => 
                                                {
                                                    return new ExtractedImage
                                                        {
                                                            src = e.GetAttributeValue("src", null),
                                                            height = GetNullableInt(e.GetAttributeValue("height", null)),
                                                            width = GetNullableInt(e.GetAttributeValue("width", null)),
                                                            alt = e.GetAttributeValue("alt", null)
                                                        };
                                                })
                                            .Where(s => !string.IsNullOrWhiteSpace(s.src) && !string.IsNullOrWhiteSpace(s.alt))
                                            .ToList();
        }
    }
}
