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
        public override async Task AddComment(string parentId, string content)
        {
            try
            {
                if (_settingsService.IsOnline() && (await _userService.GetUser()).Username != null)
                    await base.AddComment(parentId, content);
                else
                    await _offlineService.EnqueueAction("AddComment", new Dictionary<string, string> { { "parentId", parentId }, { "content", content } });
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _notificationService.CreateErrorNotification(ex);
                _offlineService.EnqueueAction("AddComment", new Dictionary<string, string> { { "parentId", parentId }, { "content", content } }).Start();
            }
            

        }

        public override async Task AddMessage(string recipient, string subject, string message)
        {
            try
            {
                if (_settingsService.IsOnline() && (await _userService.GetUser()).Username != null)
                    await base.AddMessage(recipient, subject, message);
                else
                    await _offlineService.EnqueueAction("AddMessage", new Dictionary<string, string> { { "recipient", recipient }, { "subject", subject }, { "message", message } });
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _notificationService.CreateErrorNotification(ex);
                _offlineService.EnqueueAction("AddMessage", new Dictionary<string, string> { { "recipient", recipient }, { "subject", subject }, { "message", message } }).Start();
            }
        }

        public override async Task AddPost(string kind, string url, string subreddit, string title)
        {
            try
            {
                if (_settingsService.IsOnline() && (await _userService.GetUser()).Username != null)
                    await base.AddPost(kind, url, subreddit, title);
                else
                    await _offlineService.EnqueueAction("AddPost", new Dictionary<string, string> 
                    { 
                        { "kind", kind }, 
                        { "url", url }, 
                        { "subreddit", subreddit }, 
                        { "title", title } 
                    });
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _notificationService.CreateErrorNotification(ex);
                _offlineService.EnqueueAction("AddPost", new Dictionary<string, string> 
                    { 
                        { "kind", kind }, 
                        { "url", url }, 
                        { "subreddit", subreddit }, 
                        { "title", title } 
                    }).Start();
            }
        }

        public override async Task AddVote(string thingId, int direction)
        {
            try
            {
                if (_settingsService.IsOnline() && (await _userService.GetUser()).Username != null)
                    await base.AddVote(thingId, direction);
                else
                    await _offlineService.EnqueueAction("AddVote", new Dictionary<string, string> { { "thingId", thingId }, { "direction", direction.ToString() } });
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _notificationService.CreateErrorNotification(ex);
                _offlineService.EnqueueAction("AddVote", new Dictionary<string, string> { { "thingId", thingId }, { "direction", direction.ToString() } }).Start();
            }
        }

        public override async Task AddSubredditSubscription(string subreddit, bool unsub)
        {
            try
            {
                if (_settingsService.IsOnline() && (await _userService.GetUser()).Username != null)
                    await base.AddSubredditSubscription(subreddit, unsub);
                else
                    await _offlineService.EnqueueAction("AddVote", new Dictionary<string, string> { { "subreddit", subreddit }, { "direcunsubtion", unsub.ToString() } });
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _notificationService.CreateErrorNotification(ex);
                _offlineService.EnqueueAction("AddVote", new Dictionary<string, string> { { "subreddit", subreddit }, { "direcunsubtion", unsub.ToString() } }).Start();
            }
        }

        public override async Task AddSavedThing(string thingId)
        {
            try
            {
                if (_settingsService.IsOnline() && (await _userService.GetUser()).Username != null)
                    await base.AddSavedThing(thingId);
                else
                    await _offlineService.EnqueueAction("AddSavedThing", new Dictionary<string, string> { { "thingId", thingId } });
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _notificationService.CreateErrorNotification(ex);
                _offlineService.EnqueueAction("AddSavedThing", new Dictionary<string, string> { { "thingId", thingId } }).Start();
            }
        }

        public override async Task AddReportOnThing(string thingId)
        {
            try
            {
                if (_settingsService.IsOnline() && (await _userService.GetUser()).Username != null)
                    await base.AddReportOnThing(thingId);
                else
                    await _offlineService.EnqueueAction("AddReportOnThing", new Dictionary<string, string> { { "thingId", thingId } });
            }
            catch (TaskCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _notificationService.CreateErrorNotification(ex);
                _offlineService.EnqueueAction("AddReportOnThing", new Dictionary<string, string> { { "thingId", thingId } }).Start();
            }
        }

        ThreadPoolTimer _queueTimer;
        public async Task RunQueue(ThreadPoolTimer timer)
        {
            try
            {
                if (_settingsService.IsOnline() && (await _userService.GetUser()).Username != null)
                {
                    var actionTpl = await _offlineService.DequeueAction();
                    if (actionTpl != null)
                    {
                        switch (actionTpl.Item1)
                        {
                            case "AddComment":
                                {
                                    await AddComment(actionTpl.Item2["parentId"], actionTpl.Item2["content"]);
                                    break;
                                }
                            case "AddMessage":
                                {
                                    await AddMessage(actionTpl.Item2["recipient"], actionTpl.Item2["subject"], actionTpl.Item2["message"]);
                                    break;
                                }
                            case "AddPost":
                                {
                                    await AddPost(actionTpl.Item2["kind"], actionTpl.Item2["url"], actionTpl.Item2["subreddit"], actionTpl.Item2["title"]);
                                    break;
                                }
                            case "AddVote":
                                {
                                    await AddVote(actionTpl.Item2["thingId"], int.Parse(actionTpl.Item2["direction"]));
                                    break;
                                }
                            case "AddSubredditSubscription":
                                {
                                    await AddSubredditSubscription(actionTpl.Item2["subreddit"], bool.Parse(actionTpl.Item2["direction"]));
                                    break;
                                }
                            case "AddSavedThing":
                                {
                                    await AddSavedThing(actionTpl.Item2["thingId"]);
                                    break;
                                }
                            case "AddReportOnThing":
                                {
                                    await AddReportOnThing(actionTpl.Item2["thingId"]);
                                    break;
                                }
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.ToString());
            }
            //we dont need to be particularly active here, as we dont want to burn battery when nothing is happening and we dont want to choke out
            //the content requests when the user is actively browsing around
            _queueTimer = ThreadPoolTimer.CreateTimer(async (timerParam) => await RunQueue(timerParam), new TimeSpan(0, 0, 2));
        }

        Task<Listing> _subredditsListing;
        public override Task<Listing> GetSubreddits(int? limit)
        {
            if (_subredditsListing == null)
                _subredditsListing = base.GetSubreddits(limit);

            return _subredditsListing;
        }
    }
}