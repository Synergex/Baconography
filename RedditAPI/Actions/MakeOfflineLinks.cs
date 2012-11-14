using Baconography.OfflineStore;
using Baconography.RedditAPI.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;

namespace Baconography.RedditAPI.Actions
{
    class MakeOfflineLinks
    {
        public int Limit { get; set; }
        public string BaseUrl { get; set; }

        public async Task Run(User loggedInUser)
        {
            try
            {
				TriggerToast(string.Format("Downloading content for offline viewing."));

                await (await Comments.GetInstance()).Clear();
                await (await Links.GetInstance()).Clear();

                var linkGetter = new GetAdditionalFromListing { BaseURL = BaseUrl };

                List<Listing> gottenLinkBlocks = new List<Listing>();

                int gottenCount = 0;
                while (gottenCount < Limit)
                {
                    var linkBlock = await linkGetter.Run(loggedInUser);
                    if (linkBlock != null)
                    {
                        gottenLinkBlocks.Add(linkBlock);
                        await (await Links.GetInstance()).StoreLinks(linkBlock);

                        if (linkBlock.Data.After == null)
                            break;

                        gottenCount += linkBlock.Data.Children.Count;
                        linkGetter.After = linkBlock.Data.After;
                    }

                }

                TriggerToast(string.Format("{0} Links now available offline", gottenCount));
                //this is where we should kick off the non reddit content getter/converter on a seperate thread

                var remainingMoreThings = new List<Tuple<Link, TypedThing<More>>>();

                foreach (var linkBlock in gottenLinkBlocks)
                {
                    foreach (var link in linkBlock.Data.Children)
                    {
                        var linkData = link.Data as Link;
                        var commentGetter = new GetCommentsOnPost { Subreddit = linkData.Subreddit, PermaLink = linkData.Permalink, Limit = 500 };
                        var comments = await commentGetter.Run(loggedInUser);
                        if (comments != null)
                        {
							if (comments.Data.Children.Count == 0)
							{
								throw new Exception();
							}
                            await (await Comments.GetInstance()).StoreComments(comments);
                            var moreChild = comments.Data.Children.LastOrDefault(comment => comment.Data is More);
                            if (moreChild != null)
                            {
                                TypedThing<More> moreThing = new TypedThing<More>(moreChild);
                                if (moreThing != null && moreThing.Data.Children.Count > 0)
                                {
                                    if (moreThing.Data.Children.Count > loggedInUser.MaxTopLevelOfflineComments)
                                    {
                                        moreThing.Data.Children.RemoveRange(loggedInUser.MaxTopLevelOfflineComments, moreThing.Data.Children.Count - loggedInUser.MaxTopLevelOfflineComments - 1);
                                    }

                                    remainingMoreThings.Add(Tuple.Create(linkData, moreThing));
                                }
                            }
                        }
                    }
                }

                TriggerToast("Inital comments for offline links now available");

                //we've seperated getting the links and initial comments because we want to prioritize getting some data for all of the links instead of all the data for a very small number of links
                //ex, someone getting on a plane in 5 minutes wants to get what they can on a broad a selection of links as possible, rather than all of the comments on the latest 10 bazilion comment psy ama

                if (!loggedInUser.OfflineOnlyGetsFirstSet)
                {

                    uint commentCount = 0;
                    foreach (var moreThingTpl in remainingMoreThings)
                    {
                        var moreThing = moreThingTpl.Item2;
                        var linkData = moreThingTpl.Item1;

                        while (moreThing != null && moreThing.Data.Children.Count > 0)
                        {
                            var moreGetter = new GetMoreOnListing { ChildrenIds = moreThing.Data.Children.Take(500).ToList(), Subreddit = linkData.Subreddit, ContentId = linkData.Name };
                            var moreComments = await moreGetter.Run(loggedInUser);
                            var moreMoreComments = moreComments.Data.Children.FirstOrDefault(thing => thing.Data is More);
                            if (moreMoreComments != null)
                            {
                                //we asked for more then reddit was willing to give us back
                                //just make sure we dont lose anyone
                                moreGetter.ChildrenIds.RemoveAll((str) => ((More)moreMoreComments.Data).Children.Contains(str));
                                //all thats left is what was returned so remove them by value from the moreThing
                                moreThing.Data.Children.RemoveAll((str) => moreGetter.ChildrenIds.Contains(str));
                                commentCount += (uint)((More)moreMoreComments.Data).Children.Count;
                            }
                            else
                            {
                                moreThing.Data.Children.RemoveRange(0, moreGetter.ChildrenIds.Count);
                            }
                            await (await Comments.GetInstance()).StoreComments(moreComments);
                        }

                    }
                    TriggerToast(string.Format("{0} Top level comments for offline links now available", commentCount));
                }
            }
            catch (Exception)
            {
                User.ShowDisconnectedMessage();
            }
        }

        private void TriggerToast(string text)
        {
            ToastTemplateType toastTemplate = ToastTemplateType.ToastImageAndText01; 
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);

            XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");
            toastTextElements[0].AppendChild(toastXml.CreateTextNode(text));

			XmlElement imageNode = (XmlElement)toastXml.GetElementsByTagName("image")[0];
			imageNode.SetAttribute("id", "1");
			imageNode.SetAttribute("src", @"Assets/BaconographyKitaroPlug.png");

            IXmlNode toastNode = toastXml.SelectSingleNode("/toast"); 
            ((XmlElement)toastNode).SetAttribute("launch", "{\"type\":\"toast\" }");

            ToastNotification toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }
    }
}
