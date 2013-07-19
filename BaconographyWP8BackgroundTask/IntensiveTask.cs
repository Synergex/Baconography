using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using BaconographyWP8.PlatformServices;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

namespace BaconographyWP8BackgroundTask
{
    class IntensiveTask
    {
        public static async Task Run(Action notifyWhenComplete, Dispatcher dispatcher)
        {
            //we want to download new images for the lock/tiles
            //we want to download image api/images/links/comments from the users pinned reddits
            //so when they wake up in the morning everything is fast
            //this task only runs when we're on wifi so there shouldnt be any concerns about bandwidth
            bool inUIDispatcher = false;
            try
            {
                var baconProvider = new BaconProvider(new Tuple<Type, Object>[0]);

                await baconProvider.Initialize(null);

                var offlineService = baconProvider.GetService<IOfflineService>();
                var redditService = baconProvider.GetService<IRedditService>();
                var imagesService = baconProvider.GetService<IImagesService>();
                var settingsService = baconProvider.GetService<ISettingsService>();
                var userService = baconProvider.GetService<IUserService>();
                var pivotSubreddits = await offlineService.RetrieveOrderedThings("pivotsubreddits", TimeSpan.FromDays(1024));

                //if we dont have saved pivots we got here somehow without ever going through the main page
                if (pivotSubreddits != null)
                {
                    foreach (var pivotSubreddit in pivotSubreddits)
                    {
                        if (pivotSubreddit.Data is Subreddit && ((Subreddit)pivotSubreddit.Data).Id != null)
                        {
                            var typedThing = new TypedThing<Subreddit>(pivotSubreddit);
                            var subredditPosts = await redditService.GetPostsBySubreddit(typedThing.TypedData.Url, 100);
                            if (subredditPosts != null && subredditPosts.Data.Children != null)
                            {
                                foreach (var post in subredditPosts.Data.Children)
                                {
                                    if(post.Data is Link)
                                    {
                                        var postUrl = ((Link)post.Data).Url;
                                        if (imagesService.MightHaveImagesFromUrl(postUrl))
                                        {
                                            var images = await imagesService.GetImagesFromUrl("", postUrl);
                                            if (images != null)
                                            {
                                                foreach (var image in images)
                                                {
                                                    //the OS will cache these files appropriately once we've done a full download of them
                                                    var imageBytes = await imagesService.ImageBytesFromUrl(image.Item2);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                inUIDispatcher = true;
                dispatcher.BeginInvoke(async () =>
                    {
                        try
                        {
                            var lockScreenImages = await MakeLockScreenImages(settingsService, redditService, userService, imagesService);
                            var tileImages = await MakeTileImages(settingsService, redditService, userService, imagesService);
                            var loginCookie = (await userService.GetUser()).LoginCookie;
                            using (var taskCookieFile = File.OpenWrite(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "taskSettings.json"))
                            {
                                TaskSettings settings = new TaskSettings { cookie = loginCookie ?? "", opacity = settingsService.OverlayOpacity.ToString(), number_of_items = settingsService.OverlayItemCount.ToString(), link_reddit = settingsService.LockScreenReddit, lock_images = lockScreenImages.ToArray(), tile_images = tileImages.ToArray() };
                                var settingsBlob = JsonConvert.SerializeObject(settings);
                                var settingsBytes = Encoding.UTF8.GetBytes(settingsBlob);
                                taskCookieFile.Write(settingsBytes, 0, settingsBytes.Length);
                            }
                        }
                        catch {}
                        finally
                        {
                            notifyWhenComplete();
                        }
                    });
            }
            catch { }
            finally
            {
                if(!inUIDispatcher)
                    notifyWhenComplete();
            }
        }


        struct TaskSettings
        {
            public string cookie;
            public string opacity;
            public string number_of_items;
            public string link_reddit;
            public string[] lock_images;
            public string[] tile_images;
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

        public static async Task<IEnumerable<string>> MakeLockScreenImages(ISettingsService settingsService, IRedditService redditService, IUserService userService, IImagesService imagesService)
        {
            List<string> results = new List<string>();
            var imagesSubredditResult = await redditService.GetPostsBySubreddit(settingsService.ImagesSubreddit, 25);
            var imagesLinks = imagesSubredditResult.Data.Children;
            Shuffle(imagesLinks);

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

                        using (var stream = new MemoryStream(await imagesService.ImageBytesFromUrl(url)))
                        {
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

                        results.Add(string.Format("lockScreenCache{0}.jpg", results.Count.ToString()));
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            return results;
        }

        public static async Task<IEnumerable<string>> MakeTileImages(ISettingsService settingsService, IRedditService redditService, IUserService userService, IImagesService imagesService)
        {
            List<string> results = new List<string>();
            var linksSubredditResult = await redditService.GetPostsBySubreddit(settingsService.LockScreenReddit, 100);
            var imagesLinks = linksSubredditResult.Data.Children;
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
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            return results;
        }
    }
}
