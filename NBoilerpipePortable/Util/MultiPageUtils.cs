using Sgml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NBoilerpipePortable.Util
{
    public class MultiPageUtils
    {
        private class LinkData
        {
            public float Score;
            public string LinkText;
            public string LinkHref;
        }

        private static readonly Regex _UnlikelyCandidatesRegex = new Regex("combx|comment|community|disqus|extra|foot|header|menu|remark|rss|shoutbox|sidebar|side|sponsor|ad-break|agegate|pagination|pager|popup|tweet|twitter",  RegexOptions.IgnoreCase);
        private static readonly Regex _OkMaybeItsACandidateRegex = new Regex("and|article|body|column|main|shadow", RegexOptions.IgnoreCase);
        private static readonly Regex _PositiveWeightRegex = new Regex("article|body|content|entry|hentry|main|page|pagination|post|text|blog|story",  RegexOptions.IgnoreCase);
        private static readonly Regex _NegativeWeightRegex = new Regex("combx|comment|com-|contact|foot|footer|footnote|masthead|media|meta|outbrain|promo|related|scroll|shoutbox|sidebar|side|sponsor|shopping|tags|tool|widget",  RegexOptions.IgnoreCase);
        private static readonly Regex _NegativeLinkParentRegex = new Regex("(stories|articles|news|documents|posts|notes|series|historie|artykuly|artykuły|wpisy|dokumenty|serie|geschichten|erzählungen|erzahlungen)",  RegexOptions.IgnoreCase);
        private static readonly Regex _Extraneous = new Regex("print|archive|comment|discuss|e[-]?mail|share|reply|all|login|sign|single|also",  RegexOptions.IgnoreCase);
        private static readonly Regex _DivToPElementsRegex = new Regex("<(a|blockquote|dl|div|img|ol|p|pre|table|ul)",  RegexOptions.IgnoreCase);
        private static readonly Regex _EndOfSentenceRegex = new Regex("\\.( |$)",  RegexOptions.Multiline);
        private static readonly Regex _BreakBeforeParagraphRegex = new Regex("<br[^>]*>\\s*<p", RegexOptions.None);
        private static readonly Regex _NormalizeSpacesRegex = new Regex("\\s{2,}", RegexOptions.None);
        private static readonly Regex _KillBreaksRegex = new Regex("(<br\\s*\\/?>(\\s|&nbsp;?)*){1,}", RegexOptions.None);
        private static readonly Regex _VideoRegex = new Regex("http:\\/\\/(www\\.)?(youtube|vimeo)\\.com",  RegexOptions.IgnoreCase);
        private static readonly Regex _ReplaceDoubleBrsRegex = new Regex("(<br[^>]*>[ \\n\\r\\t]*){2,}",  RegexOptions.IgnoreCase);
        private static readonly Regex _ReplaceFontsRegex = new Regex("<(\\/?)font[^>]*>",  RegexOptions.IgnoreCase);
        private static readonly Regex _ArticleTitleDashRegex1 = new Regex(" [\\|\\-] ", RegexOptions.None);
        private static readonly Regex _ArticleTitleDashRegex2 = new Regex("(.*)[\\|\\-] .*", RegexOptions.None);
        private static readonly Regex _ArticleTitleDashRegex3 = new Regex("[^\\|\\-]*[\\|\\-](.*)", RegexOptions.None);
        private static readonly Regex _ArticleTitleColonRegex1 = new Regex(".*:(.*)", RegexOptions.None);
        private static readonly Regex _ArticleTitleColonRegex2 = new Regex("[^:]*[:](.*)", RegexOptions.None);
        private static readonly Regex _NextLink = new Regex(@"(next|weiter|continue|dalej|następna|nastepna>([^\|]|$)|�([^\|]|$))",  RegexOptions.IgnoreCase);
        private static readonly Regex _NextStoryLink = new Regex("(story|article|news|document|post|note|series|historia|artykul|artykuł|wpis|dokument|seria|geschichte|erzählung|erzahlung|artikel|serie)",  RegexOptions.IgnoreCase);
        private static readonly Regex _PrevLink = new Regex("(prev|earl|[^b]old|new|wstecz|poprzednia|<|�)",  RegexOptions.IgnoreCase);
        private static readonly Regex _PageRegex = new Regex("pag(e|ing|inat)|([^a-z]|^)pag([^a-z]|$)",  RegexOptions.IgnoreCase);
        private static readonly Regex _LikelyParagraphDivRegex = new Regex("text|para|parbase",  RegexOptions.IgnoreCase);
        private static readonly Regex _MailtoHrefRegex = new Regex("^\\s*mailto\\s*:", RegexOptions.IgnoreCase);
        private static readonly Regex _TitleWhitespacesCleanUpRegex = new Regex("\\s+", RegexOptions.None);

        /// <summary>
        /// Looks for any paging links that may occur within the document
        /// </summary>
        /// <param name="body">Content body</param>
        /// <param name="url">Url of document</param>
        public static string FindNextPageLink(XElement body, string url)
        {
            Dictionary<string, LinkData> possiblePagesByLink = new Dictionary<string, LinkData>();
            IEnumerable<XElement> allLinks = GetElementsByTagName(body, "a");
            string articleBaseUrl = FindBaseUrl(url);

            /* Loop through all links, looking for hints that they may be next-page links. 
             * Things like having "page" in their textContent, className or id, or being a child
             * of a node with a page-y className or id. 
             * After we do that, assign each page a score.
             */
            foreach (XElement linkElement in allLinks)
            {
                string linkHref = (string)linkElement.Attribute("href");

                if (string.IsNullOrEmpty(linkHref)
                 || _MailtoHrefRegex.IsMatch(linkHref))
                {
                    continue;
                }

                linkHref = Regex.Replace(linkHref, "#.*$", "");
                linkHref = Regex.Replace(linkHref, "/$", "");

                /* If we've already seen this page, then ignore it. */
                // This leaves out an already-checked page check, because 
                // the web transcoder is seperate from the original transcoder
                if (linkHref == "" || linkHref == articleBaseUrl || linkHref == url)
                {
                    continue;
                }

                /* If it's on a different domain, skip it. */
                Uri linkHrefUri;

                if (Uri.TryCreate(linkHref, UriKind.Absolute, out linkHrefUri) && linkHrefUri.Host != new Uri(articleBaseUrl).Host)
                {
                    continue;
                }

                string linkText = GetInnerText(linkElement);

                /* If the linktext looks like it's not the next page, then skip it */
                if (_Extraneous.IsMatch(linkText))
                {
                    continue;
                }

                /* If the leftovers of the URL after removing the base URL don't contain any digits, it's certainly not a next page link. */
                string linkHrefLeftover = linkHref.Replace(articleBaseUrl, "");

                if (!Regex.IsMatch(linkHrefLeftover, @"\d"))
                {
                    continue;
                }

                if (!possiblePagesByLink.Keys.Contains(linkHref))
                {
                    possiblePagesByLink[linkHref] = new LinkData { Score = 0, LinkHref = linkHref, LinkText = linkText };
                }
                else
                {
                    possiblePagesByLink[linkHref].LinkText += " | " + linkText;
                }

                LinkData linkObj = possiblePagesByLink[linkHref];

                /*
                 * If the articleBaseUrl isn't part of this URL, penalize this link. It could still be the link, but the odds are lower.
                 * Example: http://www.actionscript.org/resources/articles/745/1/JavaScript-and-VBScript-Injection-in-ActionScript-3/Page1.html
                 */
                if (linkHref.IndexOf(articleBaseUrl, StringComparison.OrdinalIgnoreCase) == -1)
                {
                    linkObj.Score -= 50;
                }

                string linkData = linkText + " " + GetClass(linkElement) + " " + GetId(linkElement);

                if (_NextLink.IsMatch(linkData)
                && !_NextStoryLink.IsMatch(linkData))
                {
                    linkObj.Score += 50;
                }

                if (_PageRegex.IsMatch(linkData))
                {
                    linkObj.Score += 25;
                }

                /* If we already matched on "next", last is probably fine. If we didn't, then it's bad. Penalize. */
                /* -65 is enough to negate any bonuses gotten from a > or � in the text */
                if (Regex.IsMatch(linkData, "(first|last)", RegexOptions.IgnoreCase)
                 && !_NextLink.IsMatch(linkObj.LinkText))
                {
                    linkObj.Score -= 65;
                }

                if (_NegativeWeightRegex.IsMatch(linkData) || _Extraneous.IsMatch(linkData))
                {
                    linkObj.Score -= 50;
                }

                if (_PrevLink.IsMatch(linkData))
                {
                    linkObj.Score -= 200;
                }

                /* If any ancestor node contains page or paging or paginat */
                XElement parentNode = linkElement.Parent;
                bool positiveNodeMatch = false;
                bool negativeNodeMatch = false;

                while (parentNode != null)
                {
                    string parentNodeClassAndId = GetClass(parentNode) + " " + GetId(parentNode);

                    if (!positiveNodeMatch && (_PageRegex.IsMatch(parentNodeClassAndId) || _NextLink.IsMatch(parentNodeClassAndId)))
                    {
                        positiveNodeMatch = true;
                        linkObj.Score += 25;
                    }

                    if (!negativeNodeMatch && (_NegativeWeightRegex.IsMatch(parentNodeClassAndId) || _NegativeLinkParentRegex.IsMatch(parentNodeClassAndId)))
                    {
                        if (!_PositiveWeightRegex.IsMatch(parentNodeClassAndId))
                        {
                            linkObj.Score -= 25;
                            negativeNodeMatch = true;
                        }
                    }

                    parentNode = parentNode.Parent;
                }

                /* If any descendant node contains 'next indicator' or 'prev indicator' - adjust the score */
                bool positiveDescendantMatch = false;
                bool negativeDescendantMatch = false;

                foreach (XElement descendantElement in linkElement.Descendants())
                {
                    string descendantData = GetInnerText(descendantElement) + " " + GetClass(descendantElement) + " " + GetId(descendantElement) + " " + GetAttributeValue(descendantElement, "alt", "");

                    if (!positiveDescendantMatch && _NextLink.IsMatch(descendantData))
                    {
                        linkObj.Score += 12.5f;
                        positiveDescendantMatch = true;
                    }

                    if (!negativeDescendantMatch && _PrevLink.IsMatch(descendantData))
                    {
                        linkObj.Score -= 100;
                        negativeDescendantMatch = true;
                    }
                }

                /*
                * If the URL looks like it has paging in it, add to the score.
                * Things like /page/2/, /pagenum/2, ?p=3, ?page=11, ?pagination=34
                */
                if (Regex.IsMatch(linkHref, @"p(a|g|ag)?(e|ing|ination)?(=|\/)[0-9]{1,2}", RegexOptions.IgnoreCase)
                 || Regex.IsMatch(linkHref, @"(page|paging)", RegexOptions.IgnoreCase)
                 || Regex.IsMatch(linkHref, @"section", RegexOptions.IgnoreCase))
                {
                    linkObj.Score += 25;
                }

                /* If the URL contains negative values, give a slight decrease. */
                if (_Extraneous.IsMatch(linkHref))
                {
                    linkObj.Score -= 15;
                }

                /*
                 * If the link text can be parsed as a number, give it a minor bonus, with a slight
                 * bias towards lower numbered pages. This is so that pages that might not have 'next'
                 * in their text can still get scored, and sorted properly by score.
                 */
                int linkTextAsNumber;
                bool isInt = int.TryParse(linkText, out linkTextAsNumber);

                if (isInt)
                {
                    /* Punish 1 since we're either already there, or it's probably before what we want anyways. */
                    if (linkTextAsNumber == 1)
                    {
                        linkObj.Score -= 10;
                    }
                    else
                    {
                        linkObj.Score += Math.Max(0, 10 - linkTextAsNumber);
                    }
                }
            }

            /*
            * Loop through all of our possible pages from above and find our top candidate for the next page URL.
            * Require at least a score of 50, which is a relatively high confidence that this page is the next link.
            */
            LinkData topPage = null;

            foreach (string page in possiblePagesByLink.Keys)
            {
                if (possiblePagesByLink[page].Score >= 50 && (topPage == null || topPage.Score < possiblePagesByLink[page].Score))
                {
                    topPage = possiblePagesByLink[page];
                }
            }

            if (topPage != null)
            {
                string nextHref = Regex.Replace(topPage.LinkHref, @"\/$", "");
                var nextHrefUri = new Uri(new Uri(articleBaseUrl), nextHref);

                return nextHrefUri.OriginalString;
            }

            return null;
        }

        internal static string FindBaseUrl(string url)
        {
            Uri urlUri;

            if (!Uri.TryCreate(url, UriKind.Absolute, out urlUri))
            {
                return url;
            }

            string protocol = urlUri.Scheme;
            string hostname = urlUri.Host;
            string noUrlParams = urlUri.AbsolutePath + "/";
            List<string> urlSlashes = noUrlParams.Split('/').Reverse().ToList();
            var cleanedSegments = new List<string>();
            int slashLen = urlSlashes.Count();

            for (int i = 0; i < slashLen; i++)
            {
                string segment = urlSlashes[i];

                /* Split off and save anything that looks like a file type. */
                if (segment.IndexOf('.') != -1)
                {
                    string possibleType = segment.Split('.')[1];

                    /* If the type isn't alpha-only, it's probably not actually a file extension. */
                    if (!Regex.IsMatch(possibleType, "[a-zA-Z]"))
                    {
                        segment = segment.Split('.')[0];
                    }
                }

                /*
                 * EW-CMS specific segment replacement. Ugly.
                 * Example: http://www.ew.com/ew/article/0,,20313460_20369436,00.html
                */
                if (segment.IndexOf(",00") != -1)
                {
                    segment = segment.Replace(",00", "");
                }

                /* If our first or second segment has anything looking like a page number, remove it. */
                var pageNumRegex = new Regex("((_|-)?p[a-z]*|(_|-))[0-9]{1,2}$", RegexOptions.IgnoreCase);

                if (pageNumRegex.IsMatch(segment) && ((i == 1) || (i == 0)))
                {
                    segment = pageNumRegex.Replace(segment, "");
                }

                /* If this is purely a number, and it's the first or second segment, it's probably a page number. Remove it. */
                bool del = (i < 2 && Regex.IsMatch(segment, @"^[\d]{1,2}$"));

                /* If this is the first segment and it's just "index," remove it. */
                if (i == 0 && segment.ToLower() == "index")
                {
                    del = true;
                }

                /* If tour first or second segment is smaller than 3 characters, and the first segment was purely alphas, remove it. */
                // TODO: Check these "purely alpha" regexes.  They don't seem right.
                if (i < 2 && segment.Length < 3 && !Regex.IsMatch(urlSlashes[0], "[a-z]", RegexOptions.IgnoreCase))
                {
                    del = true;
                }

                /* If it's not marked for deletion, push it to cleanedSegments */
                if (!del)
                {
                    cleanedSegments.Add(segment);
                }
            }

            /* This is our final, cleaned, base article URL. */
            cleanedSegments.Reverse();

            return string.Format("{0}://{1}{2}", protocol, hostname, String.Join("/", cleanedSegments.ToArray()));
        }

        public static IEnumerable<XElement> GetElementsByTagName(XContainer container, string tagName)
        {
            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            if (string.IsNullOrEmpty(tagName))
            {
                throw new ArgumentNullException("tagName");
            }

            return container.Descendants()
              .Where(e => tagName.Equals(e.Name.LocalName, StringComparison.OrdinalIgnoreCase));
        }

        public static string GetClass(XElement element)
        {
            return GetAttributeValue(element, "class", "");
        }

        public static string GetId(XElement element)
        {
            return GetAttributeValue(element, "id", "");
        }

        public static string GetAttributeValue(XElement element, string attributeName, string defaultValue)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }

            if (string.IsNullOrEmpty(attributeName))
            {
                throw new ArgumentNullException("attributeName");
            }

            var attribute = element.Attribute(attributeName);

            return attribute != null
                     ? (attribute.Value ?? defaultValue)
                     : defaultValue;
        }

        internal static string GetInnerText(XNode node, bool dontNormalizeSpaces)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            string result;

            if (node is XElement)
            {
                result = ((XElement)node).Value;
            }
            else if (node is XText)
            {
                result = ((XText)node).Value;
            }
            else
            {
                throw new NotSupportedException(string.Format("Nodes of type '{0}' are not supported.", node.GetType()));
            }

            result = (result ?? "").Trim();

            if (!dontNormalizeSpaces)
            {
                return _NormalizeSpacesRegex.Replace(result, " ");
            }

            return result;
        }

        internal static string GetInnerText(XNode node)
        {
            return GetInnerText(node, false);
        }
    }
    public class SgmlDomBuilder
    {
        #region Public methods

        public static XElement GetBody(XDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }

            var documentRoot = document.Root;

            if (documentRoot == null)
            {
                return null;
            }

            return MultiPageUtils.GetElementsByTagName(documentRoot, "body").FirstOrDefault();
        }

        /// <summary>
        /// Constructs a DOM (System.Xml.Linq.XDocument) from HTML markup.
        /// </summary>
        /// <param name="htmlContent">HTML markup from which the DOM is to be constructed.</param>
        /// <returns>System.Linq.Xml.XDocument instance which is a DOM of the provided HTML markup.</returns>
        public static XDocument BuildDocument(string htmlContent)
        {
            if (htmlContent == null)
            {
                throw new ArgumentNullException("htmlContent");
            }

            if (htmlContent.Trim().Length == 0)
            {
                return new XDocument();
            }

            // "trim end" htmlContent to ...</html>$ (codinghorror.com puts some scripts after the </html> - sic!)
            const string htmlEnd = "</html";
            int indexOfHtmlEnd = htmlContent.LastIndexOf(htmlEnd);

            if (indexOfHtmlEnd != -1)
            {
                int indexOfHtmlEndBracket = htmlContent.IndexOf('>', indexOfHtmlEnd);

                if (indexOfHtmlEndBracket != -1)
                {
                    htmlContent = htmlContent.Substring(0, indexOfHtmlEndBracket + 1);
                }
            }

            XDocument document;

            try
            {
                document = LoadDocument(htmlContent);
            }
            catch (InvalidOperationException exc)
            {
                // sometimes SgmlReader doesn't handle <script> tags well and XDocument.Load() throws,
                // so we can retry with the html content with <script> tags stripped off

                if (!exc.Message.Contains("EndOfFile"))
                {
                    throw;
                }

                htmlContent = HtmlUtils.RemoveScriptTags(htmlContent);

                document = LoadDocument(htmlContent);
            }

            return document;
        }

        private static XDocument LoadDocument(string htmlContent)
        {
            using (var sgmlReader = new SgmlReader())
            {
                sgmlReader.CaseFolding = CaseFolding.ToLower;
                sgmlReader.DocType = "HTML";

                using (var sr = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(htmlContent))))
                {
                    sgmlReader.InputStream = sr;

                    var document = XDocument.Load(sgmlReader);

                    return document;
                }
            }
        }

        #endregion
    }
    public static class HtmlUtils
    {
        public static string RemoveScriptTags(string htmlContent)
        {
            if (htmlContent == null)
            {
                throw new ArgumentNullException("htmlContent");
            }

            if (htmlContent.Length == 0)
            {
                return "";
            }

            int indexOfScriptTagStart = htmlContent.IndexOf("<script", StringComparison.OrdinalIgnoreCase);

            if (indexOfScriptTagStart == -1)
            {
                return htmlContent;
            }

            int indexOfScriptTagEnd = htmlContent.IndexOf("</script>", indexOfScriptTagStart, StringComparison.OrdinalIgnoreCase);

            if (indexOfScriptTagEnd == -1)
            {
                return htmlContent.Substring(0, indexOfScriptTagStart);
            }

            string strippedHtmlContent =
              htmlContent.Substring(0, indexOfScriptTagStart) +
              htmlContent.Substring(indexOfScriptTagEnd + "</script>".Length);

            return RemoveScriptTags(strippedHtmlContent);
        }
    }
}
