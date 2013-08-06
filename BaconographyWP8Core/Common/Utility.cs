using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using BaconographyWP8.Converters;
using BaconographyWP8.PlatformServices;
using BaconographyWP8.ViewModel;
using BaconographyWP8BackgroundControls.View;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Scheduler;
using Microsoft.Practices.ServiceLocation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Windows.Networking.Connectivity;

namespace BaconographyWP8.Common
{
	public class Utility
	{
		public static SolidColorBrush GetColorFromHexa(string hexaColor)
		{
			return new SolidColorBrush(
				Color.FromArgb(
					Convert.ToByte(hexaColor.Substring(1, 2), 16),
					Convert.ToByte(hexaColor.Substring(3, 2), 16),
					Convert.ToByte(hexaColor.Substring(5, 2), 16),
					Convert.ToByte(hexaColor.Substring(7, 2), 16)
				)
			);
		}

        const string ReadMailGlyph = "\uE166";
        const string UnreadMailGlyph = "\uE119";

        struct TaskSettings
        {
            public string cookie;
            public string opacity;
            public string number_of_items;
            public string link_reddit;
            public string live_reddit;
            public string[] lock_images;
            public string[] tile_images;
        }

        private static bool loadingActiveLockScreen = false;

        private static string CleanRedditLink(string userInput, User user)
        {
            if (userInput == "/")
                return userInput;

            if (user != null && !string.IsNullOrWhiteSpace(user.Username))
            {
                var selfMulti = "/" + user.Username + "/m/";
                if (userInput.Contains(selfMulti))
                {
                    return "/me/" + userInput.Substring(userInput.IndexOf(selfMulti) + selfMulti.Length);
                }
            }

            if (userInput.StartsWith("me/m/"))
                return "/" + userInput;
            else if (userInput.StartsWith("/m/"))
                return "/me" + userInput;
            else if (userInput.StartsWith("/me/m/"))
                return userInput;

            if (userInput.StartsWith("/u/"))
            {
                return userInput.Replace("/u/", "/user/");
            }

            if (userInput.StartsWith("r/"))
                return "/" + userInput;
            else if (userInput.StartsWith("/") && !userInput.StartsWith("/r/"))
                return "/r" + userInput;
            else if (userInput.StartsWith("/r/"))
                return userInput;
            else
                return "/r/" + userInput;
        }


        public static async Task DoActiveLockScreen(ISettingsService settingsService, IRedditService redditService, IUserService userService, IImagesService imagesService, INotificationService notificationService, bool supressInit)
        {
            try
            {
                if (loadingActiveLockScreen)
                    return;

                var connectionProfile = NetworkInformation.GetInternetConnectionProfile();
                var connectionCostType = connectionProfile.GetConnectionCost().NetworkCostType;

                loadingActiveLockScreen = true;
                Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = true });

                var user = await userService.GetUser();
                var loginCookie = user.LoginCookie;

                IEnumerable<string> lockScreenImages = new string[0];
                IEnumerable<string> tileImages = new string[0];

                if ((settingsService.UpdateImagesOnlyOnWifi && Microsoft.Phone.Net.NetworkInformation.DeviceNetworkInformation.IsWiFiEnabled) ||
                    (connectionCostType != NetworkCostType.Variable))
                {
                    if(!settingsService.UseImagePickerForLockScreen)
                        lockScreenImages = await MakeLockScreenImages(settingsService, redditService, userService, imagesService);
                    tileImages = await MakeTileImages(settingsService, redditService, userService, imagesService);

                }
                else if(File.Exists(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "taskSettings.json"))
                {
                    //find the images we used last time
                    var taskSettings = LoadTaskSettings();
                    if (taskSettings != null)
                    {
                        if (!settingsService.UseImagePickerForLockScreen)
                            lockScreenImages = taskSettings.Value.lock_images;
                        tileImages = taskSettings.Value.tile_images;
                    }
                }

                if (settingsService.UseImagePickerForLockScreen)
                {
                    lockScreenImages = new string[] { Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockScreenCache0.jpg" };
                }

                using (var taskCookieFile = File.Create(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "taskSettings.json"))
                {
                    TaskSettings settings = new TaskSettings { cookie = loginCookie ?? "", opacity = settingsService.OverlayOpacity.ToString(), number_of_items = settingsService.OverlayItemCount.ToString(), link_reddit = CleanRedditLink(settingsService.LockScreenReddit, user), live_reddit = CleanRedditLink(settingsService.LiveTileReddit, user), lock_images = lockScreenImages.ToArray(), tile_images = tileImages.ToArray() };
                    var settingsBlob = JsonConvert.SerializeObject(settings);
                    var settingsBytes = Encoding.UTF8.GetBytes(settingsBlob);
                    taskCookieFile.Write(settingsBytes, 0, settingsBytes.Length);
                }

                //this can happen when the user is still trying to use the application so dont lock up the UI thread with this work
                await Task.Yield();

                var lockScreenViewModel = await MakeLockScreenControl(settingsService, redditService, userService, lockScreenImages);

                //nasty nasty hack for stupid platform limitation, no data binding if you're not in the visual tree
                var lockScreenView = new LockScreenViewControl(lockScreenViewModel);
                lockScreenView.Width = settingsService.ScreenWidth;
                lockScreenView.Height = settingsService.ScreenHeight;
                lockScreenView.UpdateLayout();
                lockScreenView.Measure(new Size(settingsService.ScreenWidth, settingsService.ScreenHeight));
                lockScreenView.Arrange(new Rect(0, 0, settingsService.ScreenWidth, settingsService.ScreenHeight));
                WriteableBitmap bitmap = new WriteableBitmap(settingsService.ScreenWidth, settingsService.ScreenHeight);
                bitmap.Render(lockScreenView, new ScaleTransform() { ScaleX = 1, ScaleY = 1 });
                bitmap.Invalidate();
                string targetFilePath = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreen.jpg";
                if (File.Exists(targetFilePath))
                {
                    targetFilePath = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreenAlt.jpg";
                }

                using (var lockscreenJpg = File.Create(targetFilePath))
                {
                    bitmap.SaveJpeg(lockscreenJpg, settingsService.ScreenWidth, settingsService.ScreenHeight, 0, 100);
                    lockscreenJpg.Flush(true);
                    lockscreenJpg.Close();
                }

                //this can happen when the user is still trying to use the application so dont lock up the UI thread with this work
                await Task.Yield();

                LockHelper(Path.GetFileName(targetFilePath), false, supressInit);

                if (targetFilePath.EndsWith("lockscreenAlt.jpg") && File.Exists(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreen.jpg"))
                {
                    File.Delete(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreen.jpg");
                }
                else if (targetFilePath.EndsWith("lockscreen.jpg") && File.Exists(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreenAlt.jpg"))
                {
                    File.Delete(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreenAlt.jpg");
                }

                if(settingsService.EnableUpdates)
                    StartPeriodicAgent();

                if(settingsService.EnableOvernightUpdates)
                    StartIntensiveAgent();
                

            }
            catch
            {
                notificationService.CreateNotification("There was an error while setting the lock screen");
            }
            finally
            {
                loadingActiveLockScreen = false;
                Messenger.Default.Send<LoadingMessage>(new LoadingMessage { Loading = false });
            }
        }

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
                if (periodicTask.LastExitReason == AgentExitReason.None && periodicTask.IsScheduled)
                {
                    //ScheduledActionService.LaunchForTest(periodicTaskName, TimeSpan.FromSeconds(20));
                    return;
                }

                //if (periodicTask.LastExitReason != AgentExitReason.Completed)
                //{
                    //MessageBox.Show(periodicTask.LastExitReason.ToString());
                //}

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
                //ScheduledActionService.LaunchForTest(periodicTaskName, TimeSpan.FromSeconds(20));
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
                //ScheduledActionService.LaunchForTest(intensiveTaskName, TimeSpan.FromSeconds(60));
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

        static TaskSettings? LoadTaskSettings()
        {
            try
            {
                using (var settingsFile = File.OpenRead(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "taskSettings.json"))
                {
                    byte[] taskCookieBytes = new byte[4096];
                    var readBytes = settingsFile.Read(taskCookieBytes, 0, 4096);
                    var json = Encoding.UTF8.GetString(taskCookieBytes, 0, readBytes);
                    var taskSettings = JsonConvert.DeserializeObject<TaskSettings>(json);
                    return taskSettings;
                }
            }
            catch
            {
                //bad file dont know how it got messed up but kill it
                if (File.Exists(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "taskSettings.json"))
                {
                    File.Delete(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "taskSettings.json");
                }
                return null;
            }
        }

        public static async Task<IEnumerable<string>> MakeLockScreenImages(ISettingsService settingsService, IRedditService redditService, IUserService userService, IImagesService imagesService)
        {
            try
            {
                //find the images we used last time
                var lockScreenSettings = LoadTaskSettings();

                if (lockScreenSettings != null && lockScreenSettings.Value.lock_images != null && lockScreenSettings.Value.lock_images.Length > 0)
                {
                    var dateTime = File.GetLastWriteTime(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockScreenCache0.jpg");
                    if ((DateTime.Now - dateTime).TotalDays < 3)
                        return lockScreenSettings.Value.lock_images;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            List<string> results = new List<string>();
            var imagesSubredditResult = await redditService.GetPostsBySubreddit(CleanRedditLink(settingsService.ImagesSubreddit, await userService.GetUser()), 100);
            var imagesLinks = new List<Thing>(imagesSubredditResult.Data.Children);

            imagesLinks.Select(thing => thing.Data is Link && imagesService.IsImage(((Link)thing.Data).Url)).ToList();
            if (imagesLinks.Count > 0)
            {
                //download images one at a time, check resolution
                //set LockScreenViewModel properties
                //render to bitmap
                //save bitmap
                BitmapImage imageSource = null;
                for (int i = 0; i < imagesLinks.Count; i++)
                {
                    if (!(imagesLinks[i].Data is Link))
                        continue;

                    try
                    {
                        var url = ((Link)imagesLinks[i].Data).Url;
                        imageSource = new BitmapImage();
                        imageSource.CreateOptions = BitmapCreateOptions.None;

                        var imagesList = await imagesService.GetImagesFromUrl("", url);
                        if (imagesList == null || imagesList.Count() == 0)
                            continue;

                        url = imagesList.First().Item2;

                        using (var stream = await ImagesService.ImageStreamFromUrl(url))
                        {
                            try
                            {
                                if (url.EndsWith(".jpg") || url.EndsWith(".jpeg"))
                                {
                                    var dimensions = GetJpegDimensions(stream);
                                    stream.Seek(0, SeekOrigin.Begin);
                                    //bigger than 16 megs when loaded means we need to chuck it
                                    if (dimensions == null || (dimensions.Height * dimensions.Width * 4) > 16 * 1024 * 1024)
                                        continue;
                                }
                                else if (stream.Length > 1024 * 1024) //its too big drop it
                                {
                                    continue;
                                }
                            }
                            catch
                            {
                                if (stream.Length > 1024 * 1024) //its too big drop it
                                {
                                    continue;
                                }
                            }

                            imageSource.SetSource(stream);
                        }
                        if (imageSource.PixelHeight == 0 || imageSource.PixelWidth == 0)
                            continue;

                        if (settingsService.HighresLockScreenOnly
                            && (imageSource.PixelHeight < 800
                                || imageSource.PixelWidth < 480))
                            continue;

                        Image lockScreenView = new Image();
                        lockScreenView.Width = 480;
                        lockScreenView.Height = 800;
                        lockScreenView.Source = imageSource;
                        lockScreenView.Stretch = Stretch.UniformToFill;
                        lockScreenView.UpdateLayout();
                        lockScreenView.Measure(new Size(480, 800));
                        lockScreenView.Arrange(new Rect(0, 0, 480, 800));
                        WriteableBitmap bitmap = new WriteableBitmap(480, 800);
                        bitmap.Render(lockScreenView, new ScaleTransform() { ScaleX = 1, ScaleY = 1 });
                        bitmap.Invalidate();

                        using (var theFile = File.Create(Windows.Storage.ApplicationData.Current.LocalFolder.Path + string.Format("\\lockScreenCache{0}.jpg", results.Count.ToString())))
                        {
                            bitmap.SaveJpeg(theFile, 480, 800, 0, 100);
                            theFile.Flush(true);
                            theFile.Close();
                        }
                        //this can happen when the user is still trying to use the application so dont lock up the UI thread with this work
                        await Task.Yield();
                        results.Add(string.Format("lockScreenCache{0}.jpg", results.Count.ToString()));

                        if (results.Count > 10)
                            break;
                    }
                    catch (OutOfMemoryException oom)
                    {
                        //we're done here
                        break;
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            return results;
        }

        public static Dimensions GetJpegDimensions(Stream fs)
        {
            if (!fs.CanSeek) throw new ArgumentException("Stream must be seekable");
            long blockStart;
            var buf = new byte[4];
            fs.Read(buf, 0, 4);
            if (buf.SequenceEqual(new byte[] { 0xff, 0xd8, 0xff, 0xe0 }))
            {
                blockStart = fs.Position;
                fs.Read(buf, 0, 2);
                var blockLength = ((buf[0] << 8) + buf[1]);
                fs.Read(buf, 0, 4);
                if (Encoding.UTF8.GetString(buf, 0, 4) == "JFIF"
                    && fs.ReadByte() == 0)
                {
                    blockStart += blockLength;
                    while (blockStart < fs.Length)
                    {
                        fs.Position = blockStart;
                        fs.Read(buf, 0, 4);
                        blockLength = ((buf[2] << 8) + buf[3]);
                        if (blockLength >= 7 && buf[0] == 0xff && buf[1] == 0xc0)
                        {
                            fs.Position += 1;
                            fs.Read(buf, 0, 4);
                            var height = (buf[0] << 8) + buf[1];
                            var width = (buf[2] << 8) + buf[3];
                            return new Dimensions(width, height);
                        }
                        blockStart += blockLength + 2;
                    }
                }
            }
            return null;
        }

        public class Dimensions
        {
            private readonly int width;
            private readonly int height;
            public Dimensions(int width, int height)
            {
                this.width = width;
                this.height = height;
            }
            public int Width
            {
                get { return width; }
            }
            public int Height
            {
                get { return height; }
            }
            public override string ToString()
            {
                return string.Format("width:{0}, height:{1}", Width, Height);
            }
        }

        public static async Task<IEnumerable<string>> MakeTileImages(ISettingsService settingsService, IRedditService redditService, IUserService userService, IImagesService imagesService)
        {
            List<string> results = new List<string>();
            var linksSubredditResult = await redditService.GetPostsBySubreddit(CleanRedditLink(settingsService.LiveTileReddit, await userService.GetUser()), 100);
            var imagesLinks = new List<Thing>(linksSubredditResult.Data.Children);

            if (imagesLinks.Count > 0)
            {
                //download images one at a time, check resolution
                //set LockScreenViewModel properties
                //render to bitmap
                //save bitmap
                BitmapImage imageSource = null;
                for (int i = 0; i < imagesLinks.Count; i++)
                {
                    if (!(imagesLinks[i].Data is Link))
                        continue;

                    try
                    {
                        var url = ((Link)imagesLinks[i].Data).Url;
                        imageSource = new BitmapImage();
                        imageSource.CreateOptions = BitmapCreateOptions.None;

                        var imagesList = await imagesService.GetImagesFromUrl("", url);
                        if (imagesList == null || imagesList.Count() == 0)
                            continue;

                        url = imagesList.First().Item2;

                        using (var stream = new MemoryStream(await imagesService.ImageBytesFromUrl(url)))
                        {
                            imageSource.SetSource(stream);
                        }
                        if (imageSource.PixelHeight == 0 || imageSource.PixelWidth == 0)
                            continue;


                        Image lockScreenView = new Image();
                        lockScreenView.Width = 691;
                        lockScreenView.Height = 336;
                        lockScreenView.Source = imageSource;
                        lockScreenView.Stretch = Stretch.UniformToFill;
                        lockScreenView.UpdateLayout();
                        lockScreenView.Measure(new Size(691, 336));
                        lockScreenView.Arrange(new Rect(0, 0, 691, 336));
                        WriteableBitmap bitmap = new WriteableBitmap(691, 336);
                        bitmap.Render(lockScreenView, new ScaleTransform() { ScaleX = 1, ScaleY = 1 });
                        bitmap.Invalidate();

                        using (var store = IsolatedStorageFile.GetUserStoreForApplication())
                        {
                            var filename = string.Format("/Shared/ShellContent/tileCache{0}.jpg", results.Count.ToString());
                            using (var st = new IsolatedStorageFileStream(filename, FileMode.Create, FileAccess.Write, store))
                            {
                                bitmap.SaveJpeg(st, 691, 336, 0, 90);
                            }
                        }

                        results.Add(string.Format("tileCache{0}.jpg", results.Count.ToString()));
                        if (results.Count > 17)
                            break;

                        //this can happen when the user is still trying to use the application so dont lock up the UI thread with this work
                        await Task.Yield();
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            return results;
        }

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
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        public static async Task<LockScreenViewModel> MakeLockScreenControl(ISettingsService settingsService, IRedditService redditService, IUserService userService, IEnumerable<string> lockScreenImages)
        {
            var user = (await userService.GetUser());
            if (user.Me != null && (user.Me.HasMail || user.Me.HasModMail))
            {
                //toast the user that they have mail
                //ServiceLocator.Current.GetInstance<INotificationService>().CreateNotification("you have new mail");
            }
            
            LinkGlyphConverter linkGlyphConverter = new LinkGlyphConverter();
            List<LockScreenMessage> lockScreenMessages = new List<LockScreenMessage>();

            //maybe call for messages from logged in user
            if (user != null && user.LoginCookie != null && settingsService.MessagesInLockScreenOverlay)
            {
                var messages = await redditService.GetMessages(null);
                lockScreenMessages.AddRange(messages.Data.Children.Where(thing => thing.Data is Message && ((Message)thing.Data).New).Take(3).Select(thing => new LockScreenMessage
                {
                    DisplayText = thing.Data is CommentMessage ? ((CommentMessage)thing.Data).LinkTitle : ((Message)thing.Data).Subject,
                    Glyph = ((Message)thing.Data).New ? UnreadMailGlyph : ReadMailGlyph
                }));
            }

            if (settingsService.PostsInLockScreenOverlay && settingsService.OverlayItemCount > 0)
            {
                //call for posts from selected subreddit (defaults to front page)
                var frontPageResult = new List<Thing>((await redditService.GetPostsBySubreddit(CleanRedditLink(settingsService.LockScreenReddit, user), 10)).Data.Children);
                Shuffle(frontPageResult);
                lockScreenMessages.AddRange(frontPageResult.Where(thing => thing.Data is Link).Take(settingsService.OverlayItemCount - lockScreenMessages.Count).Select(thing => new LockScreenMessage { DisplayText = ((Link)thing.Data).Title, Glyph = linkGlyphConverter != null ? (string)linkGlyphConverter.Convert(((Link)thing.Data), typeof(String), null, System.Globalization.CultureInfo.CurrentCulture) : "" }));
            }

            List<string> shuffledLockScreenImages = new List<string>(lockScreenImages);
            Shuffle(shuffledLockScreenImages);

            var vml = new ViewModelLocator().LockScreen;
            vml.ImageSource = shuffledLockScreenImages.FirstOrDefault();
            vml.OverlayItems = lockScreenMessages;
            vml.OverlayOpacity = settingsService.OverlayOpacity;
            vml.NumberOfItems = settingsService.OverlayItemCount;
            return vml;
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
