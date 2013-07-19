using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using BaconographyWP8.Converters;
using BaconographyWP8.PlatformServices;
using BaconographyWP8.ViewModel;
using BaconographyWP8BackgroundControls.View;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.ServiceLocation;
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
using Windows.Networking.Connectivity;

namespace BaconographyWP8.Common
{
	class Utility
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
            public string[] lock_images;
            public string[] tile_images;
        }

        private static bool loadingActiveLockScreen = false;
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

                var loginCookie = (await userService.GetUser()).LoginCookie;

                IEnumerable<string> lockScreenImages = new string[0];
                IEnumerable<string> tileImages = new string[0];

                if (settingsService.UpdateImagesOnlyOnWifi && connectionCostType == NetworkCostType.Unrestricted)
                {
                    lockScreenImages = await MakeLockScreenImages(settingsService, redditService, userService, imagesService);
                    tileImages = await MakeTileImages(settingsService, redditService, userService, imagesService);

                }
                else if(File.Exists(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "taskSettings.json"))
                {
                    //find the images we used last time
                    using (var settingsFile = File.OpenRead(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "taskSettings.json"))
                    {
                        byte[] taskCookieBytes = new byte[4096];
                        var readBytes = settingsFile.Read(taskCookieBytes, 0, 4096);
                        var json = Encoding.UTF8.GetString(taskCookieBytes, 0, readBytes);
                        var taskSettings = JsonConvert.DeserializeObject<TaskSettings>(json);
                        lockScreenImages = taskSettings.lock_images;
                        tileImages = taskSettings.tile_images;
                    }
                }
                using (var taskCookieFile = File.OpenWrite(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "taskSettings.json"))
                {
                    TaskSettings settings = new TaskSettings { cookie = loginCookie ?? "", opacity = settingsService.OverlayOpacity.ToString(), number_of_items = settingsService.OverlayItemCount.ToString(), link_reddit = settingsService.LockScreenReddit, lock_images = lockScreenImages.ToArray(), tile_images = tileImages.ToArray() };
                    var settingsBlob = JsonConvert.SerializeObject(settings);
                    var settingsBytes = Encoding.UTF8.GetBytes(settingsBlob);
                    taskCookieFile.Write(settingsBytes, 0, settingsBytes.Length);
                }


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
                var lockscreenJpg = File.Create(targetFilePath);
                bitmap.SaveJpeg(lockscreenJpg, settingsService.ScreenWidth, settingsService.ScreenHeight, 0, 100);
                lockscreenJpg.Flush(true);
                lockscreenJpg.Close();

                BackgroundTask.LockHelper(Path.GetFileName(targetFilePath), false, supressInit);

                if (targetFilePath.EndsWith("lockscreenAlt.jpg") && File.Exists(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreen.jpg"))
                {
                    File.Delete(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreen.jpg");
                }
                else if (targetFilePath.EndsWith("lockscreen.jpg") && File.Exists(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreenAlt.jpg"))
                {
                    File.Delete(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreenAlt.jpg");
                }
                if(!supressInit)
                    BackgroundTask.StartPeriodicAgent();

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
                var frontPageResult = await redditService.GetPostsBySubreddit(settingsService.LockScreenReddit, 10);
                Shuffle(frontPageResult.Data.Children);
                lockScreenMessages.AddRange(frontPageResult.Data.Children.Where(thing => thing.Data is Link).Take(settingsService.OverlayItemCount - lockScreenMessages.Count).Select(thing => new LockScreenMessage { DisplayText = ((Link)thing.Data).Title, Glyph = linkGlyphConverter != null ? (string)linkGlyphConverter.Convert(((Link)thing.Data), typeof(String), null, System.Globalization.CultureInfo.CurrentCulture) : "" }));
            }

            List<string> shuffledLockScreenImages = new List<string>(lockScreenImages);
            Shuffle(shuffledLockScreenImages);

            var vml = new ViewModelLocator().LockScreen;
            vml.ImageSource = shuffledLockScreenImages.FirstOrDefault();
            vml.OverlayItems = lockScreenMessages;
            vml.OverlayOpacity = settingsService.OverlayOpacity;
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
