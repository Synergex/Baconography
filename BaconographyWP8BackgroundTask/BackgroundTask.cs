﻿using BaconographyWP8.ViewModel;
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
using System.IO.IsolatedStorage;
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
        public static readonly string periodicTaskName = "LockScreen_Updater";
        public static readonly string intensiveTaskName = "Intensive_Baconography_Updater";

        private string userInfoDbPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\userinfodb.ism";
        //we must be very carefull how much memory is used during this, we are limited to 10 megs or we get shutdown
        //dont fully initialize things, just the bare minimum to get the job done
        protected override async void OnInvoke(ScheduledTask task)
        {
            string lockScreenImage = "lockScreenCache1.jpg";
            List<object> tileImages = new List<object>();
            string linkReddit = "/";
            string liveReddit = "/";
            int opacity = 35;
            int numberOfItems = 6;
            TinyRedditService redditService = null;
            bool hasMail = false;
            bool roundedCorners = false;
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

                        var rounded = JSON.GetValue(decodedJson, "rounded") as bool?;
                        var cookie = JSON.GetValue(decodedJson, "cookie") as string;
                        var opacityStr = JSON.GetValue(decodedJson, "opacity") as string;
                        var numOfItemsStr = JSON.GetValue(decodedJson, "number_of_items") as string;
                        linkReddit = (JSON.GetValue(decodedJson, "link_reddit") as string) ?? "/";
                        liveReddit = (JSON.GetValue(decodedJson, "live_reddit") as string) ?? "/";
                        var lockScreenImages = JSON.GetValue(decodedJson, "lock_images") as List<object>;
                        tileImages = JSON.GetValue(decodedJson, "tile_images") as List<object>;

                        Shuffle(lockScreenImages);
                        lockScreenImage = (lockScreenImages.FirstOrDefault() as string) ?? "lockScreenCache1.jpg";

                        if (!Int32.TryParse(opacityStr, out opacity)) opacity = 35;
                        if (!Int32.TryParse(numOfItemsStr, out numberOfItems)) numberOfItems = 6;
                        if (rounded != null) roundedCorners = rounded.Value;

                        if (!string.IsNullOrWhiteSpace(cookie))
                        {
                            redditService = new TinyRedditService(null, null, cookie);
                            hasMail = true;// dont make two calls when we can just ask for unread messages once
                        }
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
            lockScreenViewModel.RoundedCorners = roundedCorners;

            if (task.Name == periodicTaskName)
            {
                if (hasMail)
                {
                    //get messages and notify user if a new message has appeared
                    //add messages to the list if they are unread
                    var messagesTpl = await redditService.GetNewMessages(null);
                    var messages = messagesTpl.Item1;
                    messageCount = messagesTpl.Item2;
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
                                toast.NavigationUri = new Uri("/BaconographyWP8Core;component/View/MessagingPageView.xaml?refresh", UriKind.Relative);
                                toast.Show();
                            }
                            lockScreenViewModel.OverlayItems.Add(new LockScreenMessage { DisplayText = message, Glyph = "\uE119" });
                        }
                    }
                    messagesTpl = null;
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

                List<Tuple<string, string>> liveTileImageUrls = new List<Tuple<string, string>>();
                var liveTileLinks = await redditService.GetPostsBySubreddit(liveReddit, 100);
                foreach (var link in liveTileLinks)
                {
                    if (link.Item2.EndsWith(".jpg") || link.Item2.EndsWith(".jpeg") || link.Item2.EndsWith(".png"))
                    {
                        liveTileImageUrls.Add(link);
                    }

                    //pick up a few extra just in case we have to throw some away due to sizing
                    if (liveTileImageUrls.Count > 20)
                        break;
                }

                liveTileLinks = null;
                links = null;

                Debug.WriteLine(DeviceStatus.ApplicationCurrentMemoryUsage);

                for (int i = 0; i < 2; i++)
                {
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                    GC.WaitForPendingFinalizers();
                }

                Deployment.Current.Dispatcher.BeginInvoke(async () =>
                {
                    for (int i = 0; i < 2; i++)
                    {
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                        GC.WaitForPendingFinalizers();
                    }
                    BuildLockScreen(tileImages, messageCount, lockScreenViewModel);
                    //it appears to take a few runs to knock down the memory, probably a native reference counting issue
                    //thats why we also have to wait for pending finalizers
                    for (int i = 0; i < 2; i++)
                    {
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                        GC.WaitForPendingFinalizers();
                    }
                    int liveTileCounter = 0;

                    foreach (var existingLiveTileImage in tileImages)
                    {
                        try
                        {
                            var dateTime = File.GetLastWriteTime(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\Shared\\ShellContent\\" + existingLiveTileImage);
                            if ((DateTime.Now - dateTime).TotalHours < 4)
                            {
                                //dont nuke files newer than 4 hours
                                //also file ages should grow as the number gets larger
                                liveTileCounter++;
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.ToString());
                        }
                    }

                    int startTileCounter = liveTileCounter;
                    var liveTileLink = liveTileImageUrls.FirstOrDefault();
                    try
                    {
                        liveTileCounter = await BuildTileImage(redditService, liveTileCounter, liveTileLink);
                        for (int i = 0; i < 2; i++)
                        {
                            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                            GC.WaitForPendingFinalizers();
                        }
                    }
                    catch { }

                    try
                    {
                        UpdateLiveTile(tileImages, messageCount, liveTileCounter, startTileCounter);
                    }
                    catch { }

                    //DONE
                    NotifyComplete();
                });
            }
            else
            {
                try
                {
                    //do the resource intensive task
                    //cache as many images as we can for later
                    //try to make new live tile images, we might OOM though so catch and quit

                    var links = await redditService.GetPostsBySubreddit(linkReddit, 100);
                    if (links != null)
                    {
                        var cacheBuffer = new byte[1024];
                        foreach (var link in links)
                        {
                            //we should probably try to put some kind of primative version of the image api logic here
                            if (link.Item2.EndsWith(".jpg") || link.Item2.EndsWith(".jpeg") || link.Item2.EndsWith(".png"))
                            {
                                using (var cacheStream = await redditService.CacheUrl(link.Item2)) 
                                {
                                    while (cacheStream != null && cacheStream.CanRead && cacheStream.Length > cacheStream.Position)
                                    {
                                        //things get stuffed up if we dont read the bytes from the requests
                                        cacheStream.Read(cacheBuffer, 0, 1024);
                                    }
                                };
                            }
                        }
                    }

                    var liveTileLinks = links;
                    if (linkReddit != liveReddit)
                    {
                        liveTileLinks = await redditService.GetPostsBySubreddit(liveReddit, 100);
                    }

                    Deployment.Current.Dispatcher.BeginInvoke(async () =>
                    {
                        try
                        {
                            int liveTileCounter = 0;
                            foreach (var link in liveTileLinks)
                            {
                                //we dont rewrite settings because we dont have enough ram to load the neccisary dlls
                                //so there is no point in downloading more live tile images then we started with
                                if (liveTileCounter >= tileImages.Count)
                                    break;

                                if (link.Item2.EndsWith(".jpg") || link.Item2.EndsWith(".jpeg") || link.Item2.EndsWith(".png"))
                                {
                                    try
                                    {
                                        liveTileCounter = await BuildTileImage(redditService, liveTileCounter, link);
                                        for (int i = 0; i < 2; i++)
                                        {
                                            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                                            GC.WaitForPendingFinalizers();
                                        }
                                    }
                                    catch 
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                        catch { }
                        finally
                        {
                            NotifyComplete();
                        }
                    });
                }
                catch
                {
                    NotifyComplete();
                }
            }
        }

        public static void UpdateLiveTile(List<object> tileImages, int messageCount, int liveTileCounter, int startTileCounter)
        {
            var activeTiles = ShellTile.ActiveTiles;
            var activeTile = activeTiles.FirstOrDefault();
            if (activeTile != null)
            {
                var uris = new List<Uri>();

                Shuffle(tileImages);

                if (startTileCounter != liveTileCounter)
                {
                    uris.Add(new Uri(string.Format("isostore:/Shared/ShellContent/tileCache{0}.jpg", startTileCounter), UriKind.Absolute));
                }

                foreach (var image in tileImages.Take(startTileCounter != liveTileCounter ? 8 : 9))
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

        private static void BuildLockScreen(List<object> tileImages, int messageCount, LockScreenViewModel lockScreenViewModel)
        {
            try
            {
                //nasty nasty hack for stupid platform limitation, no data binding if you're not in the visual tree
                var lockScreenView = new LockScreenViewControl(lockScreenViewModel);
                lockScreenView.Width = 480;
                lockScreenView.Height = 800;
                lockScreenView.UpdateLayout();
                lockScreenView.Measure(new Size(480, 800));
                lockScreenView.Arrange(new Rect(0, 0, 480, 800));
                WriteableBitmap bitmap = new WriteableBitmap(480, 800);
                bitmap.Render(lockScreenView, new ScaleTransform { ScaleX = 1.0f, ScaleY = 1.0f });
                bitmap.Invalidate();
                lockScreenView.Dispose();
                lockScreenView = null; //nuke the UI just incase the jpeg encoder overruns memory
                string targetFilePath = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreen.jpg";
                if (File.Exists(targetFilePath))
                {
                    targetFilePath = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreenAlt.jpg";
                }
                using (var lockscreenJpg = File.Create(targetFilePath))
                {
                    bitmap.SaveJpeg(lockscreenJpg, 480, 800, 0, 100);
                    lockscreenJpg.Flush(true);
                    lockscreenJpg.Close();
                }
                BackgroundTask.LockHelper(Path.GetFileName(targetFilePath), false, true);

                if (targetFilePath.EndsWith("lockscreenAlt.jpg") && File.Exists(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreen.jpg"))
                {
                    File.Delete(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreen.jpg");
                }
                else if (targetFilePath.EndsWith("lockscreen.jpg") && File.Exists(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreenAlt.jpg"))
                {
                    File.Delete(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreenAlt.jpg");
                }
            }
            catch (Exception ex)
            {
                ShellToast toast = new ShellToast();
                toast.Title = "Failure";
                toast.Content = ex.ToString();
                toast.Show();
            }
        }

        private static async Task<int> BuildTileImage(TinyRedditService redditService, int liveTileCounter, Tuple<string, string> link)
        {
            var imageSource = new BitmapImage();
            imageSource.CreateOptions = BitmapCreateOptions.None;
            if(! await redditService.SetSourceUrl(link.Item2, imageSource))
            {
                return liveTileCounter;
            }

            if (imageSource.PixelHeight == 0 || imageSource.PixelWidth == 0)
                return liveTileCounter;

            //snag as many images as we can into existing live tile slots
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
            imageSource.UriSource = null;
            lockScreenView.Source = null;
            using (var store = IsolatedStorageFile.GetUserStoreForApplication())
            {
                var filename = string.Format("/Shared/ShellContent/tileCache{0}.jpg", liveTileCounter.ToString());
                using (var st = new IsolatedStorageFileStream(filename, FileMode.Create, FileAccess.Write, store))
                {
                    bitmap.SaveJpeg(st, 691, 336, 0, 90);
                    liveTileCounter++;
                }
            }
            return liveTileCounter;
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
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
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
