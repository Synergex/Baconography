using Baconography.NeutralServices.KitaroDB;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using KitaroDB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.System.Threading;

namespace Baconography.NeutralServices
{
    class OfflineDelayableRedditService : RedditService
    {
        public override async void AddComment(string parentId, string content)
        {
            if (_settingsService.IsOnline())
                base.AddComment(parentId, content);
            else
                await _offlineService.EnqueueAction("AddComment", new Dictionary<string, string> { { "parentId", parentId }, { "content", content } });
            
        }

        public override async void AddMessage(string recipient, string subject, string message)
        {
            if (_settingsService.IsOnline())
                base.AddMessage(recipient, subject, message);
            else
                await _offlineService.EnqueueAction("AddMessage", new Dictionary<string, string> { { "recipient", recipient }, { "subject", subject }, { "message", message } });
        }

        public override async void AddPost(string kind, string url, string subreddit, string title)
        {
            if (_settingsService.IsOnline())
                base.AddPost(kind, url, subreddit, title);
            else
                await _offlineService.EnqueueAction("AddPost", new Dictionary<string, string> 
                { 
                    { "kind", kind }, 
                    { "url", url }, 
                    { "subreddit", subreddit }, 
                    { "title", title } 
                });
        }

        public override async void AddVote(string thingId, int direction)
        {
            if (_settingsService.IsOnline())
                base.AddVote(thingId, direction);
            else
                await _offlineService.EnqueueAction("AddVote", new Dictionary<string, string> { { "thingId", thingId }, { "direction", direction.ToString() } });
        }

        public override async void AddSubredditSubscription(string subreddit, bool unsub)
        {
            if (_settingsService.IsOnline())
                base.AddSubredditSubscription(subreddit, unsub);
            else
                await _offlineService.EnqueueAction("AddVote", new Dictionary<string, string> { { "subreddit", subreddit }, { "direcunsubtion", unsub.ToString() } });
        }

        public override async void AddSavedThing(string thingId)
        {
            if (_settingsService.IsOnline())
                base.AddSavedThing(thingId);
            else
                await _offlineService.EnqueueAction("AddSavedThing", new Dictionary<string, string> { { "thingId", thingId } });
        }

        public override async void AddReportOnThing(string thingId)
        {
            if (_settingsService.IsOnline())
                base.AddReportOnThing(thingId);
            else
                await _offlineService.EnqueueAction("AddReportOnThing", new Dictionary<string, string> { { "thingId", thingId } });
        }

        ThreadPoolTimer _queueTimer;
        public async Task RunQueue(ThreadPoolTimer timer)
        {
            try
            {
                var actionTpl = await _offlineService.DequeueAction();
                if (actionTpl != null)
                {
                    switch (actionTpl.Item1)
                    {
                        case "AddComment":
                            {
                                base.AddComment(actionTpl.Item2["parentId"], actionTpl.Item2["content"]);
                                break;
                            }
                        case "AddMessage":
                            {
                                base.AddMessage(actionTpl.Item2["recipient"], actionTpl.Item2["subject"], actionTpl.Item2["message"]);
                                break;
                            }
                        case "AddPost":
                            {
                                base.AddPost(actionTpl.Item2["kind"], actionTpl.Item2["url"], actionTpl.Item2["subreddit"], actionTpl.Item2["title"]);
                                break;
                            }
                        case "AddVote":
                            {
                                base.AddVote(actionTpl.Item2["thingId"], int.Parse(actionTpl.Item2["direction"]));
                                break;
                            }
                        case "AddSubredditSubscription":
                            {
                                base.AddSubredditSubscription(actionTpl.Item2["subreddit"], bool.Parse(actionTpl.Item2["direction"]));
                                break;
                            }
                        case "AddSavedThing":
                            {
                                base.AddSavedThing(actionTpl.Item2["thingId"]);
                                break;
                            }
                        case "AddReportOnThing":
                            {
                                base.AddReportOnThing(actionTpl.Item2["thingId"]);
                                break;
                            }
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
            //we dont need to be particularly active here, as we dont want to burn battery when nothing is happening and we dont want to choke out
            //the content requests when the user is actively browsing around
            _queueTimer = ThreadPoolTimer.CreateTimer(async (timerParam) => await RunQueue(timerParam), new TimeSpan(0, 0, 2));
        }
    }
}