using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using BaconographyWP8.Converters;
using BaconographyWP8.PlatformServices;
using BaconographyWP8.View;
using BaconographyWP8.ViewModel;
using KitaroDB;
using Microsoft.Phone.Scheduler;
using System;
using System.Collections.Generic;
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
    class BackgroundTask : ScheduledTaskAgent
    {
        class BaconProvider : IBaconProvider
        {
            Dictionary<Type, object> _services;
            public BaconProvider(Dictionary<Type, object> services)
            {
                _services = services;
            }
            public T GetService<T>() where T : class
            {
                return _services[typeof(T)] as T;
            }
        }

        class DummyNavigationService : INavigationService
        {
            public void GoBack()
            {
                throw new NotImplementedException();
            }

            public void GoForward()
            {
                throw new NotImplementedException();
            }

            public bool Navigate<T>(object parameter)
            {
                throw new NotImplementedException();
            }

            public bool Navigate(Type source, object parameter)
            {
                throw new NotImplementedException();
            }

            public void NavigateToSecondary(Type source, object parameter)
            {
                throw new NotImplementedException();
            }

            public void NavigateToExternalUri(Uri uri)
            {
                throw new NotImplementedException();
            }
        }

        class DummyNotificationService : INotificationService
        {
            public void CreateNotification(string text)
            {
                throw new NotImplementedException();
            }

            public void CreateKitaroDBNotification(string text)
            {
                throw new NotImplementedException();
            }

            public void CreateErrorNotification(Exception exception)
            {
                throw new NotImplementedException();
            }
        }

        class BasicOfflineService : IOfflineService
        {
            DB _settingsDb;
            Dictionary<string, string> _settingsCache;
            public async Task Initialize()
            {
                _settingsDb = await DB.CreateAsync(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\settings_v2.ism", DBCreateFlags.None);

                _settingsCache = new Dictionary<string, string>();
                //load all of the settings up front so we dont spend so much time going back and forth
                var cursor = await _settingsDb.SeekAsync(DBReadFlags.NoLock);
                if (cursor != null)
                {
                    using (cursor)
                    {
                        do
                        {
                            _settingsCache.Add(cursor.GetKeyString(), cursor.GetString());
                        } while (await cursor.MoveNextAsync());
                    }
                }
            }
            public Task Clear()
            {
                throw new NotImplementedException();
            }

            public Task<IEnumerable<Tuple<string, string>>> GetImages(string uri)
            {
                throw new NotImplementedException();
            }

            public Task<byte[]> GetImage(string uri)
            {
                throw new NotImplementedException();
            }

            public Task StoreImage(byte[] bytes, string uri)
            {
                throw new NotImplementedException();
            }

            public Task StoreImages(IEnumerable<Tuple<string, string>> apiResults, string uri)
            {
                throw new NotImplementedException();
            }

            public Task StoreComments(Listing listing)
            {
                throw new NotImplementedException();
            }

            public Task<Listing> GetTopLevelComments(string permalink, int count)
            {
                throw new NotImplementedException();
            }

            public Task<Listing> GetMoreComments(string subredditId, string linkId, IEnumerable<string> ids)
            {
                throw new NotImplementedException();
            }

            public Task IncrementDomainStatistic(string domain, bool isLink)
            {
                throw new NotImplementedException();
            }

            public Task IncrementSubredditStatistic(string subredditId, bool isLink)
            {
                throw new NotImplementedException();
            }

            public Task<List<BaconographyPortable.Model.KitaroDB.ListingHelpers.DomainAggregate>> GetDomainAggregates(int maxListSize, int threshold)
            {
                throw new NotImplementedException();
            }

            public Task<List<BaconographyPortable.Model.KitaroDB.ListingHelpers.SubredditAggregate>> GetSubredditAggregates(int maxListSize, int threshold)
            {
                throw new NotImplementedException();
            }

            public Task StoreLink(Thing link)
            {
                throw new NotImplementedException();
            }

            public Task StoreLinks(Listing listing)
            {
                throw new NotImplementedException();
            }

            public Task<Listing> LinksForSubreddit(string subredditName, string after)
            {
                throw new NotImplementedException();
            }

            public Task<Listing> AllLinks(string after)
            {
                throw new NotImplementedException();
            }

            public Task StoreThing(string key, Thing link)
            {
                throw new NotImplementedException();
            }

            public Task<Thing> RetrieveThing(string key, TimeSpan maxAge)
            {
                throw new NotImplementedException();
            }

            public Task StoreOrderedThings(string key, IEnumerable<Thing> things)
            {
                throw new NotImplementedException();
            }

            public Task<IEnumerable<Thing>> RetrieveOrderedThings(string key, TimeSpan maxAge)
            {
                throw new NotImplementedException();
            }

            public Task<TypedThing<Link>> RetrieveLink(string id)
            {
                throw new NotImplementedException();
            }

            public Task<TypedThing<Link>> RetrieveLinkByUrl(string url, TimeSpan maxAge)
            {
                throw new NotImplementedException();
            }

            public Task<TypedThing<Subreddit>> RetrieveSubredditById(string id)
            {
                throw new NotImplementedException();
            }

            public Task StoreOrderedThings(IListingProvider listingProvider)
            {
                throw new NotImplementedException();
            }

            public Task StoreSetting(string name, string value)
            {
                throw new NotImplementedException();
            }

            public Task<string> GetSetting(string name)
            {
                return Task.FromResult<string>(_settingsCache[name]);
            }

            public Task StoreHistory(string link)
            {
                throw new NotImplementedException();
            }

            public Task ClearHistory()
            {
                throw new NotImplementedException();
            }

            public bool HasHistory(string link)
            {
                throw new NotImplementedException();
            }

            public Task Suspend()
            {
                throw new NotImplementedException();
            }

            public Task EnqueueAction(string actionName, Dictionary<string, string> parameters)
            {
                throw new NotImplementedException();
            }

            public Task<Tuple<string, Dictionary<string, string>>> DequeueAction()
            {
                throw new NotImplementedException();
            }

            public Task<Thing> GetSubreddit(string name)
            {
                throw new NotImplementedException();
            }

            public Task StoreSubreddit(TypedThing<Subreddit> subreddit)
            {
                throw new NotImplementedException();
            }

            public uint GetHash(string name)
            {
                throw new NotImplementedException();
            }


            public Task StoreMessages(User user, Listing listing)
            {
                throw new NotImplementedException();
            }

            public Task<Listing> GetMessages(User user)
            {
                throw new NotImplementedException();
            }

            public Task<bool> UserHasOfflineMessages(User user)
            {
                throw new NotImplementedException();
            }
        }
        //we must be very carefull how much memory is used during this, we are limited to 10 megs or we get shutdown
        //dont fully initialize things, just the bare minimum to get the job done
        protected override async void OnInvoke(ScheduledTask task)
        {
            var settingsService = new SettingsService();
            var redditService = new RedditService();
            var userService = new UserService();
            var httpService = new SimpleHttpService();
            var offlineService = new BasicOfflineService();
            var notificationService = new DummyNotificationService();
            var navigationService = new DummyNavigationService();
            var imagesService = new ImagesService();

            var baconProvider = new BaconProvider(new Dictionary<Type, Object> 
            {
                { typeof(ISettingsService), settingsService },
                { typeof(IRedditService), redditService },
                { typeof(IUserService), userService },
                { typeof(IOfflineService), offlineService },
                { typeof(INotificationService), notificationService },
                { typeof(INavigationService), navigationService },
                { typeof(ISimpleHttpService), httpService },
                { typeof(IImagesService), imagesService }
            });

            redditService.Initialize(settingsService, httpService, userService, notificationService, baconProvider);
            await offlineService.Initialize();
            await settingsService.Initialize(baconProvider);
            await userService.Initialize(baconProvider);
            //load settings from db
            //create reddit service
            //get user'
            await MakeLockScreenControl(settingsService, redditService, userService, imagesService, baconProvider);

            var lockScreenView = new LockScreen();
            WriteableBitmap bitmap = new WriteableBitmap(lockScreenView, null);
            string targetFilePath = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreen.jpg";
            if (File.Exists(targetFilePath))
            {
                targetFilePath = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreenAlt.jpg";
            }
            var lockscreenJpg = File.Create(targetFilePath, 4096);
            bitmap.SaveJpeg(lockscreenJpg, bitmap.PixelWidth, bitmap.PixelHeight, 0, 100);
            LockHelper(targetFilePath, false);

            if (targetFilePath.EndsWith("lockscreenAlt.jpg") && File.Exists(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreen.jpg"))
            {
                File.Delete(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreen.jpg");
            }
            else if (targetFilePath.EndsWith("lockscreen.jpg") && File.Exists(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreenAlt.jpg"))
            {
                File.Delete(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockscreenAlt.jpg");
            }

            //DONE
            NotifyComplete();
        }

        const string ReadMailGlyph = "\uE166";
        const string UnreadMailGlyph = "\uE119";

        public static async void LockHelper(string filePathOfTheImage, bool isAppResource)
        {
            try
            {
                var isProvider = Windows.Phone.System.UserProfile.LockScreenManager.IsProvidedByCurrentApplication;
                if (!isProvider)
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

                    // Get the URI of the lock screen background image.
                    var currentImage = Windows.Phone.System.UserProfile.LockScreen.GetImageUri();
                    System.Diagnostics.Debug.WriteLine("The new lock screen background image is set to {0}", currentImage.ToString());
                }
                else
                {
                    MessageBox.Show("You said no, so I can't update your background.");
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        public static async Task MakeLockScreenControl(ISettingsService settingsService, IRedditService redditService, IUserService userService, IImagesService imagesService, IBaconProvider baconProvider)
        {
            var user = (await userService.GetUser());
            if (user.Me != null && user.Me.HasMail)
            {
                //toast the user that they have mail
            }
            //call for posts from front page
            var frontPageResult = await redditService.GetPostsBySubreddit("/", 3);
            LinkGlyphConverter linkGlyphConverter = null;
            if (App.Current.Resources.Contains("linkGlyphConverter"))
            {
                linkGlyphConverter = App.Current.Resources["linkGlyphConverter"] as LinkGlyphConverter;
            }
            List<LockScreenMessage> lockScreenMessages = new List<LockScreenMessage>(frontPageResult.Data.Children.Select(thing => new LockScreenMessage { DisplayText = ((Link)thing.Data).Title, Glyph = linkGlyphConverter != null ? (string)linkGlyphConverter.Convert(((Link)thing.Data), typeof(String), null, System.Globalization.CultureInfo.CurrentCulture) : "" }));
            //maybe call for messages from logged in user
            if (user != null && user.LoginCookie != null)
            {

                var messages = await redditService.GetMessages(5);
                lockScreenMessages.AddRange(messages.Data.Children.Take(3).Select(thing => new LockScreenMessage {
                    DisplayText = thing.Data is CommentMessage ? ((CommentMessage)thing.Data).LinkTitle : ((Message)thing.Data).Subject,
                    Glyph = "\uE119" }));
            }
            //maybe call for images from subreddit


            if (string.IsNullOrWhiteSpace(settingsService.ImagesSubreddit))
            {
                settingsService.ImagesSubreddit = "/r/earthporn";
            }

            var imagesSubredditResult = await redditService.GetPostsBySubreddit(settingsService.ImagesSubreddit, 25);
            var imagesLinks = imagesSubredditResult.Data.Children;
            Shuffle(imagesLinks);


            imagesLinks.Select(thing => thing.Data is Link && imagesService.IsImage(((Link)thing.Data).Url)).ToList();
            if (imagesLinks.Count > 0)
            {
                Shuffle(lockScreenMessages);

                //download images one at a time, check resolution
                //set LockScreenViewModel properties
                //render to bitmap
                //save bitmap
                BitmapImage imageSource = new BitmapImage();
                imageSource.CreateOptions = BitmapCreateOptions.None;
                for (int i = 0; i < imagesLinks.Count; i++)
                {
                    try
                    {
                        var url = ((Link)imagesLinks[i].Data).Url;
                        var imageBytes = await imagesService.ImageBytesFromUrl(url);
                        if (imageBytes != null)
                        {
                            using (var stream = new MemoryStream(imageBytes))
                            {
                                stream.Seek(0, SeekOrigin.Begin);
                                imageSource.SetSource(stream);
                            }
                        }

                        if (imageSource.PixelHeight == 0 || imageSource.PixelWidth == 0)
                            continue;

                        if (settingsService.HighresLockScreenOnly
                            && (imageSource.PixelHeight < settingsService.ScreenHeight
                                || imageSource.PixelWidth < settingsService.ScreenWidth))
                            continue;
                    }
                    catch
                    {
                        continue;
                    }

                    break;
                }

                
                ViewModelLocator.Initialize(baconProvider);
                var vml = new ViewModelLocator();
                vml.LockScreen.ImageSource = imageSource;
                vml.LockScreen.OverlayItems = lockScreenMessages;
                vml.LockScreen.OverlayOpacity = settingsService.OverlayOpacity;
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
