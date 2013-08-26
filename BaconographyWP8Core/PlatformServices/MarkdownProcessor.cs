using BaconographyPortable.Services;
using SoldOutWP8;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyWP8Core.PlatformServices
{
    class MarkdownProcessor : IMarkdownProcessor
    {
        public MarkdownData Process(string markdown)
        {
            lock (this)
            {
                var processedMarkdownBlocks = new List<Tuple<bool, string, string>>();
                if (markdown.Length > 2048)
                {
                    foreach (var part in SplitText(markdown))
                    {
                        try
                        {
                            bool makeItPlain;
                            var processedMarkdown = MakeMarkdown(part, out makeItPlain);
                            processedMarkdownBlocks.Add(Tuple.Create(makeItPlain, processedMarkdown, part));
                        }
                        catch
                        {
                            processedMarkdownBlocks.Add(Tuple.Create(true, part, part));
                        }
                    }
                }
                else
                {
                    try
                    {
                        bool makeItPlain;
                        var processedMarkdown = MakeMarkdown(markdown, out makeItPlain);
                        processedMarkdownBlocks.Add(Tuple.Create(makeItPlain, processedMarkdown, markdown));
                    }
                    catch
                    {
                        processedMarkdownBlocks.Add(Tuple.Create(true, markdown, markdown));
                    }
                }

                return new MarkdownData { ProcessedMarkdownBlock = processedMarkdownBlocks };
            }

        }

        private unsafe string MakeMarkdown(string value, out bool makeItPlain)
        {
            var startingText = value;
            string markdown = null;
            fixed (char* textPtr = startingText)
            {
                var markdownPtr = SoldOut.MarkdownToXaml((uint)textPtr, (uint)startingText.Length);
                if (markdownPtr != 0)
                    markdown = new string((char*)markdownPtr);
            }


            //bad markdown (possibly due to unicode char, just pass it through plain)
            var noWhiteStartingText = startingText.Replace(" ", "").Replace("\n", "");
            var noWhiteMarkdown = markdown.Replace(" ", "").Replace("\n", "").Replace("<paragraph>", "");
            var isSame = (markdown.Length < "<paragraph></paragraph>".Length) || string.Compare(noWhiteStartingText, 0, noWhiteMarkdown, 0, noWhiteStartingText.Length) == 0;

            if (isSame)
            {
                makeItPlain = true;
                return value;
            }
            else
            {
                markdown = markdown.Trim('\n');
                if (!markdown.EndsWith("</Paragraph>"))
                {
                    var lastParagraph = markdown.LastIndexOf("</Paragraph>");
                    if (lastParagraph != -1)
                    {
                        markdown = markdown.Substring(0, lastParagraph + "</Paragraph>".Length) + "<Paragraph>" + markdown.Substring(lastParagraph + "</Paragraph>".Length + 1) + "</Paragraph>";
                    }
                }

                if (!markdown.Contains("<Paragraph>"))
                {
                    markdown = "<Paragraph>" + markdown + "</Paragraph>";
                }

                makeItPlain = false;
                return markdown;
            }
        }

        private List<string> SplitText(string semiCleanText)
        {
            List<string> textBlocks = new List<string>();
            int startIndex = 0;
            int foundIndex = semiCleanText.IndexOf('\n', 1024);
            while (startIndex < semiCleanText.Length && foundIndex != -1)
            {
                textBlocks.Add(semiCleanText.Substring(startIndex, (foundIndex - startIndex) - 1));
                startIndex = foundIndex + 1;
                if ((startIndex + 1024) > (semiCleanText.Length - 1))
                    break;
                foundIndex = semiCleanText.IndexOf('\n', startIndex + 1024);
            }

            if (startIndex < (semiCleanText.Length - 1))
            {
                textBlocks.Add(semiCleanText.Substring(startIndex));
            }

            return textBlocks;

        }

        private object MakePlain(string value)
        {
            return value.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&apos;", "'");
        }
    }
}
