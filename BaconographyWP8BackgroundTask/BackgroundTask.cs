using BaconographyWP8.ViewModel;
using BaconographyWP8BackgroundControls.View;
using BaconographyWP8BackgroundTask;
using BaconographyWP8BackgroundTask.Hacks;
using Microsoft.Phone.Info;
using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;
using Procurios.Public;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BaconographyWP8
{
    public class BackgroundTask : ScheduledTaskAgent
    {
        public static void RemoveAgent(string name)
        {
            try
            {
                ScheduledActionService.Remove(name);
            }
            catch (Exception)
            {
            }
        }

        public static readonly string periodicTaskName = "LockScreen_Updater";
        public static readonly string intensiveTaskName = "Intensive_Baconography_Updater";

        public static void StartPeriodicAgent()
        {
            

            // Obtain a reference to the period task, if one exists
            var periodicTask = ScheduledActionService.Find(periodicTaskName) as PeriodicTask;

            // If the task already exists and background agents are enabled for the
            // application, you must remove the task and then add it again to update 
            // the schedule
            if (periodicTask != null)
            {
                RemoveAgent(periodicTaskName);
            }

            periodicTask = new PeriodicTask(periodicTaskName);
            // The description is required for periodic agents. This is the string that the user
            // will see in the background services Settings page on the device.
            periodicTask.Description = "Keeps your lockscreen up to date with the latest redditing";

            // Place the call to Add in a try block in case the user has disabled agents.
            try
            {
                ScheduledActionService.Add(periodicTask);
                //ScheduledActionService.LaunchForTest(periodicTaskName, TimeSpan.FromSeconds(10));
            }
            catch (InvalidOperationException exception)
            {
                if (exception.Message.Contains("BNS Error: The action is disabled"))
                {
                    MessageBox.Show("Background agents for this application have been disabled by the user.");
                }

                if (exception.Message.Contains("BNS Error: The maximum number of ScheduledActions of this type have already been added."))
                {
                    // No user action required. The system prompts the user when the hard limit of periodic tasks has been reached.

                }
            }
            catch (SchedulerServiceException)
            {
            }
        }


        public static void StartIntensiveAgent()
        {
            

            // Obtain a reference to the period task, if one exists
            var intensiveTask = ScheduledActionService.Find(intensiveTaskName) as ResourceIntensiveTask;

            // If the task already exists and background agents are enabled for the
            // application, you must remove the task and then add it again to update 
            // the schedule
            if (intensiveTask != null)
            {
                RemoveAgent(intensiveTaskName);
            }

            intensiveTask = new ResourceIntensiveTask(intensiveTaskName);
            // The description is required for periodic agents. This is the string that the user
            // will see in the background services Settings page on the device.
            intensiveTask.Description = "This task does all of the heavy lifting for the lock screen updater and overnight offlining support";

            // Place the call to Add in a try block in case the user has disabled agents.
            try
            {
                ScheduledActionService.Add(intensiveTask);
                ScheduledActionService.LaunchForTest(intensiveTaskName, TimeSpan.FromSeconds(60));
            }
            catch (InvalidOperationException exception)
            {
                if (exception.Message.Contains("BNS Error: The action is disabled"))
                {
                    MessageBox.Show("Background agents for this application have been disabled by the user.");
                }

                if (exception.Message.Contains("BNS Error: The maximum number of ScheduledActions of this type have already been added."))
                {
                    // No user action required. The system prompts the user when the hard limit of periodic tasks has been reached.

                }
            }
            catch (SchedulerServiceException)
            {
            }
        }

        const string NavRightGlyph = "\uE0AD";
        const string PhotoGlyph = "\uE114";
        const string VideoGlyph = "\uE116";
        const string WebGlyph = "\uE128";
        const string DetailsGlyph = "\uE14C";

        string GetGlyph(string link)
        {
            try
            {
                string targetHost = "";
                string filename = "";
                Uri uri = null;

                if (string.IsNullOrEmpty(link))
                    return DetailsGlyph;

                uri = new Uri(link);
                filename = uri.AbsolutePath;
                targetHost = uri.DnsSafeHost.ToLower();

                if (targetHost == "www.youtube.com" ||
                        targetHost == "youtube.com")
                    return VideoGlyph;

                if (targetHost == "www.imgur.com" ||
                    targetHost == "imgur.com" ||
                    targetHost == "i.imgur.com" ||
                    targetHost == "min.us" ||
                    targetHost == "www.quickmeme.com" ||
                    targetHost == "i.qkme.me" ||
                    targetHost == "quickmeme.com" ||
                    targetHost == "qkme.me" ||
                    targetHost == "memecrunch.com" ||
                    targetHost == "flickr.com" ||
                    filename.EndsWith(".jpg") ||
                    filename.EndsWith(".gif") ||
                    filename.EndsWith(".png") ||
                    filename.EndsWith(".jpeg"))
                    return PhotoGlyph;
            }
            catch { }

            return WebGlyph;
        }

        private string userInfoDbPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\userinfodb.ism";
        //we must be very carefull how much memory is used during this, we are limited to 10 megs or we get shutdown
        //dont fully initialize things, just the bare minimum to get the job done
        protected override async void OnInvoke(ScheduledTask task)
        {
            
            if (task.Name == periodicTaskName)
            {
                //even though this only takes up 500k (because of the dll load + a thread stack existing in the async call) with large image compositing 
                //we dont have enough ram to fit anything other than absolute basic .net libraries, this is only an issue in periodic
                //Dictionary<string, string> settingsCache;
                //using (var settingsDb = await DB.CreateAsync(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\settings_v2.ism", DBCreateFlags.None))
                //{
                //    settingsCache = new Dictionary<string, string>();
                //    //load all of the settings up front so we dont spend so much time going back and forth
                //    var cursor = await settingsDb.SeekAsync(DBReadFlags.NoLock);
                //    if (cursor != null)
                //    {
                //        using (cursor)
                //        {
                //            do
                //            {
                //                settingsCache.Add(cursor.GetKeyString(), cursor.GetString());
                //            } while (await cursor.MoveNextAsync());
                //        }
                //    }
                //}



                string lockScreenImage = "lockScreenCache1.jpg";
                List<object> tileImages = new List<object>();
                string linkReddit = "/";
                int opacity = 35;
                int numberOfItems = 6;
                TinyRedditService redditService = null;
                bool hasMail = false;
                int messageCount = 0;
                try
                {
                    if (File.Exists(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "taskSettings.json"))
                    {
                        using (var cookieFile = File.OpenRead(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "taskSettings.json"))
                        {
                            byte[] taskCookieBytes = new byte[4096];
                            int readBytes = cookieFile.Read(taskCookieBytes, 0, 4096);

                            var json = Encoding.UTF8.GetString(taskCookieBytes, 0, readBytes);
                            var decodedJson = JSON.JsonDecode(json);

                            var cookie = JSON.GetValue(decodedJson, "cookie") as string;
                            var opacityStr = JSON.GetValue(decodedJson, "opacity") as string;
                            var numOfItemsStr = JSON.GetValue(decodedJson, "number_of_items") as string;
                            linkReddit = (JSON.GetValue(decodedJson, "link_reddit") as string) ?? "/";
                            var lockScreenImages = JSON.GetValue(decodedJson, "lock_images") as List<object>;
                            tileImages = JSON.GetValue(decodedJson, "tile_images") as List<object>;

                            Shuffle(lockScreenImages);
                            lockScreenImage = (lockScreenImages.FirstOrDefault() as string) ?? "lockScreenCache1.jpg";

                            if (!Int32.TryParse(opacityStr, out opacity)) opacity = 35;
                            if (!Int32.TryParse(numOfItemsStr, out numberOfItems)) numberOfItems = 6;

                            redditService = new TinyRedditService(null, null, cookie);
                            hasMail = await redditService.HasMail();
                        }
                    }
                }
                catch 
                {
                    if (File.Exists(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "taskSettings.json"))
                        File.Delete(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "taskSettings.json");
                }

                if (redditService == null)
                    redditService = new TinyRedditService(null, null, null);


                LockScreenViewModel lockScreenViewModel = new LockScreenViewModel();

                lockScreenViewModel.ImageSource = lockScreenImage;
                lockScreenViewModel.OverlayOpacity = opacity / 100.0f;
                lockScreenViewModel.NumberOfItems = numberOfItems;
                //if (settingsCache.ContainsKey("OverlayOpacity"))
                //{
                //    lockScreenViewModel.OverlayOpacity = ((float)Int32.Parse(settingsCache["OverlayOpacity"])) / 100.0f;
                //}

                if (hasMail)
                {
                    //get messages and notify user if a new message has appeared
                    //add messages to the list if they are unread
                    var messages = await redditService.GetNewMessages(null);
                    if (messages != null)
                    {
                        bool toasted = false;
                        foreach (var message in messages)
                        {
                            if (!toasted)
                            {
                                toasted = true;
                                ShellToast toast = new ShellToast();
                                toast.Title = "New message";
                                toast.Content = message;
                                toast.NavigationUri = new Uri("/View/MessagingPageView.xaml", UriKind.Relative);
                                toast.Show();
                            }
                            messageCount++;
                            lockScreenViewModel.OverlayItems.Add(new LockScreenMessage { DisplayText = message, Glyph = "\uE119" });
                        }
                    }
                    messages = null;
                }

                var links = await redditService.GetPostsBySubreddit(linkReddit, null);
                if (links != null)
                {
                    //the goal is 6 items in the list, if thats not filled with messages then fill it with links
                    foreach (var link in links)
                    {
                        if (lockScreenViewModel.OverlayItems.Count > (numberOfItems - 1))
                            break;

                        lockScreenViewModel.OverlayItems.Add(new LockScreenMessage { DisplayText = link.Item1, Glyph = GetGlyph(link.Item2) });
                    }
                }
                redditService = null;
                links = null;
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        Debug.WriteLine(DeviceStatus.ApplicationCurrentMemoryUsage);
                        //nasty nasty hack for stupid platform limitation, no data binding if you're not in the visual tree
                        var lockScreenView = new LockScreenViewControl(lockScreenViewModel);
                        lockScreenView.Width = 480;
                        lockScreenView.Height = 800;
                        lockScreenView.UpdateLayout();
                        lockScreenView.Measure(new Size(480, 800));
                        lockScreenView.Arrange(new Rect(0, 0, 480, 800));
                        Debug.WriteLine(DeviceStatus.ApplicationCurrentMemoryUsage);
                        WriteableBitmap bitmap = new WriteableBitmap(480, 800);
                        Debug.WriteLine(DeviceStatus.ApplicationCurrentMemoryUsage);
                        bitmap.Render(lockScreenView, new ScaleTransform { ScaleX = 1.0f, ScaleY = 1.0f });
                        bitmap.Invalidate();
                        lockScreenView = null; //nuke the UI just incase the jpeg encoder overruns memory
                        string targetFilePath = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreen.jpg";
                        if (File.Exists(targetFilePath))
                        {
                            targetFilePath = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreenAlt.jpg";
                        }
                        var lockscreenJpg = File.Create(targetFilePath);
                        bitmap.SaveJpeg(lockscreenJpg, 480, 800, 0, 100);
                        lockscreenJpg.Flush(true);
                        lockscreenJpg.Close();
                        BackgroundTask.LockHelper(Path.GetFileName(targetFilePath), false, true);

                        if (targetFilePath.EndsWith("lockscreenAlt.jpg") && File.Exists(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreen.jpg"))
                        {
                            File.Delete(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreen.jpg");
                        }
                        else if (targetFilePath.EndsWith("lockscreen.jpg") && File.Exists(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreenAlt.jpg"))
                        {
                            File.Delete(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreenAlt.jpg");
                        }
                        var activeTiles = ShellTile.ActiveTiles;
                        var activeTile = activeTiles.FirstOrDefault();
                        if(activeTile != null)
                        {
                            var uris = new List<Uri>();

                            Shuffle(tileImages);

                            foreach (var image in tileImages.Take(9))
                            {
                                uris.Add(new Uri("isostore:/Shared/ShellContent/" + ((string)image), UriKind.Absolute));
                            }

                            if (uris.Count == 0)
                            {
                                uris.Add(new Uri("/Assets/BaconographyPhoneIconWide.png", UriKind.Relative));
                            }

                            CycleTileData cycleTile = new CycleTileData()
                            {
                                Title = "Baconography",
                                Count = messageCount,
                                SmallBackgroundImage = new Uri("/Assets/ApplicationIconSmall.png", UriKind.Relative),
                                CycleImages = uris
                            };
                            activeTile.Update(cycleTile);
                        }

                    }
                    catch { }

                    //DONE
                    NotifyComplete();
                });
            }
            else //must be the resource intensive task, we get 10 minutes and a normal amount of ram to work with
            {
                //we get to load of the full set of dlls we have
                await IntensiveTask.Run(NotifyComplete, Deployment.Current.Dispatcher);
            }
        }

        const string ReadMailGlyph = "\uE166";
        const string UnreadMailGlyph = "\uE119";

        public static async void LockHelper(string filePathOfTheImage, bool isAppResource, bool supressInit)
        {
            try
            {
                var isProvider = Windows.Phone.System.UserProfile.LockScreenManager.IsProvidedByCurrentApplication;
                if (!isProvider && !supressInit)
                {
                    // If you're not the provider, this call will prompt the user for permission.
                    // Calling RequestAccessAsync from a background agent is not allowed.
                    var op = await Windows.Phone.System.UserProfile.LockScreenManager.RequestAccessAsync();

                    // Only do further work if the access was granted.
                    isProvider = op == Windows.Phone.System.UserProfile.LockScreenRequestResult.Granted;
                }

                if (isProvider)
                {
                    // At this stage, the app is the active lock screen background provider.

                    // The following code example shows the new URI schema.
                    // ms-appdata points to the root of the local app data folder.
                    // ms-appx points to the Local app install folder, to reference resources bundled in the XAP package.
                    var schema = isAppResource ? "ms-appx:///" : "ms-appdata:///Local/";
                    var uri = new Uri(schema + filePathOfTheImage, UriKind.Absolute);

                    // Set the lock screen background image.
                    Windows.Phone.System.UserProfile.LockScreen.SetImageUri(uri);
                }
                else if(!supressInit)
                {
                    MessageBox.Show("You said no, so I can't update your background.");
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        public static void Shuffle<T>(IList<T> list)
        {
            Random rng = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
