using BaconographyPortable.Model;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using BaconographyWP8.Common;
using Microsoft.Phone.Shell;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace BaconographyWP8.PlatformServices
{
    class LiveTileService : ILiveTileService, IBaconService
    {
        IImagesService _imagesService;

        public async Task Initialize(IBaconProvider baconProvider)
        {
            _imagesService = baconProvider.GetService<IImagesService>();
        }

        private TaskSettings? LoadTaskSettingsImpl()
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

        public TaskSettings? LoadTaskSettings()
        {
            lock (this)
            {
                return LoadTaskSettingsImpl();
            }
        }

        public void StoreTaskSettings(Func<TaskSettings?, TaskSettings> getSettings, bool atomic)
        {
            lock (this)
            {
                var currentSettings = atomic ? LoadTaskSettingsImpl() : null;
                var newSettings = getSettings(currentSettings);

                using (var taskCookieFile = File.Create(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "taskSettings.json"))
                {
                    var settingsBlob = JsonConvert.SerializeObject(newSettings);
                    var settingsBytes = Encoding.UTF8.GetBytes(settingsBlob);
                    taskCookieFile.Write(settingsBytes, 0, settingsBytes.Length);
                }
            }
        }

        Queue<DateTime> _tileCreationDates = new Queue<DateTime>();
        public async Task MaybeCreateTile(Tuple<string, string, TypedThing<Link>> thing)
        {
            try
            {
                //we want to queue up 5 tiles to be created but we only want to replace them at a max of once every 14 mintes
                if (_tileCreationDates.Count >= 5 && (DateTime.Now - _tileCreationDates.First()).TotalMinutes < 14)
                    return;

                var linkThing = thing.Item3;
                if (linkThing == null)
                    return;

                Uri image = null;
                if (thing.Item2 != null)
                    image = new Uri(thing.Item2);
                else if (thing.Item1 != null)
                    image = new Uri(thing.Item1);

                if (_tileCreationDates.Count >= 5)
                    _tileCreationDates.Dequeue();

                _tileCreationDates.Enqueue(DateTime.Now);

                await CreateTile(linkThing.Data.Title,
                    !string.IsNullOrWhiteSpace(thing.Item1) ? new Uri(thing.Item1) : null, 
                    !string.IsNullOrWhiteSpace(thing.Item2) ? new Uri(thing.Item2) : null);
            }
            catch (Exception)
            {
                //do nothing its not really that important
            }
        }


        // Take a subreddit thing and generate a pinned secondary tile. Use the display
        // name and subreddit header image in the tile.
        public async Task CreateSecondaryTileForSubreddit(TypedThing<Subreddit> subreddit)
        {
            //try
           // {
            //    // Download and create a local copy of the header image
            //    var rawImage = await _imagesService.SaveFileFromUriAsync(new Uri(subreddit.Data.HeaderImage), subreddit.Data.DisplayName + ".jpg", "Images");
            //    // Generate a square tile appropriate image
            //    var squareImage = await _imagesService.GenerateResizedImage(rawImage, 150, 150) as StorageFile;

            //    string id = "";
            //    if (subreddit != null)
            //        id = subreddit.Data.DisplayName;


            //    var properties = (await squareImage.Properties.RetrievePropertiesAsync(new string[] { "System.ParsingPath" }));
                

            //    var tile = ShellTile.Create(new Uri(""), new StandardTileData { Title = subreddit.Data.Title, BackgroundImage = new Uri(properties["System.ParsingPath"] as string) );
            //    var tileSchedule = new ShellTileSchedule(tile);
            //    tileSchedule.
            //    SecondaryTile tile = new SecondaryTile();
            //    tile.TileOptions = TileOptions.ShowNameOnWideLogo | TileOptions.ShowNameOnLogo;
            //    tile.DisplayName = "Baconography";
            //    tile.ShortName = "/r/" + id;
            //    if (subreddit != null)
            //    {

            //        // Download and create a local copy of the header image
            //        var rawImage = await _imagesService.SaveFileFromUriAsync(new Uri(subreddit.Data.HeaderImage), subreddit.Data.DisplayName + ".jpg", "Images");
            //        // Generate a square tile appropriate image
            //        var squareImage = await _imagesService.GenerateResizedImage(rawImage, 150, 150) as StorageFile;

            //        tile.WideLogo = new Uri("ms-appdata:///local/Images/" + wideImage.Name);
            //        tile.Logo = new Uri("ms-appdata:///local/Images/" + squareImage.Name);
            //        subreddit.Data.PublicDescription = null;
            //        subreddit.Data.Description = null;
            //        subreddit.Data.Headertitle = null;
            //        tile.Arguments = JsonConvert.SerializeObject(subreddit);
            //    }
            //    else
            //    {
            //        Uri logo = new Uri("ms-appx:///Assets/Logo.png");
            //        Uri wideLogo = new Uri("ms-appx:///Assets/WideLogo.png");
            //        tile.Arguments = "/r/";
            //        tile.Logo = logo;
            //        tile.WideLogo = wideLogo;
            //    }
            //    tile.TileId = "r" + id;

            //    // Ask the user to authorize creation of the tile
            //    bool isPinned = await tile.RequestCreateAsync();
            //}
            //catch (Exception)
            //{
            //    // TODO: Do something with exceptions
            //}
        }

        public bool TileExists(string name)
        {
            return false;
            //return SecondaryTile.Exists("r" + name);
        }

        public async void RemoveSecondaryTile(string name)
        {
            //if (TileExists(name))
            //{
            //    var tile = new SecondaryTile("r" + name);
            //    await tile.RequestDeleteAsync();
            //}
        }

        // Generate a tile using the text provided (typically a link title). If an image is provided,
        // we use the correct WinRT template. Otherwise, just the text wrap template.
        private async Task CreateTile(string text, Uri smallIamge, Uri largeImage)
        {
//            bool textIsLong = text.Length > 42;
//            bool largeImageIsTall = false;
//            bool largeImageIsWide = false;
//            bool smallImageIsTall = false;
//            bool smallImageIsWide = false;

//            StorageFile largeImageFile = null;
//            StorageFile smallImageFile = null;

//            if (largeImage != null)
//            {
//                largeImageFile = (await _imagesService.SaveFileFromUriAsync(largeImage, largeImage.LocalPath, "liveTiles")) as StorageFile;
//                var imageProperties = await largeImageFile.Properties.GetImagePropertiesAsync();
//                var imageRatio = ((double)imageProperties.Height / (double)imageProperties.Width);
//                largeImageIsTall = imageRatio < .9;
//                largeImageIsWide = imageRatio > 1.34;
//            }

//            if (smallIamge != null)
//            {
//                smallImageFile = (await _imagesService.SaveFileFromUriAsync(smallIamge, smallIamge.LocalPath, "liveTiles")) as StorageFile;
//                var imageProperties = await smallImageFile.Properties.GetImagePropertiesAsync();
//                var smallImageRatio = ((double)imageProperties.Height / (double)imageProperties.Width);
//                smallImageIsTall = smallImageRatio < .9;
//                smallImageIsWide = smallImageRatio > 1.34;
//            }

//            string tileXmlString = null;

//            //small & large image, text isnt long, large image is wide small image is neither tall nor wide: TileWidePeekImage06
//            if (smallImageFile != null && largeImageFile != null && !textIsLong && largeImageIsWide && !smallImageIsTall && !smallImageIsWide)
//            {
//                var tileFormat = @"
//                <tile>
//                  <visual>
//                    <binding template='TileWidePeekImage06'>
//                      <image id='1' src='{0}' alt='{2}'/>
//                      <image id='2' src='{1}' alt='{2}'/>
//                      <text id='1'>{2}</text>
//                    </binding>
//                    <binding template='TileSquarePeekImageAndText04'>
//                      <image id='1' src='{3}' alt='{2}'/>
//                      <text id='1'>{2}</text>
//                    </binding> 
//                  </visual>
//                </tile>";

//                var sizedWideLargeImage = await _imagesService.GenerateResizedImage(largeImageFile, 310, 150, 0, 0, true) as StorageFile;
//                var sizedSquareLargeImage = await _imagesService.GenerateResizedImage(largeImageFile, 150, 150, 0, 0, true) as StorageFile;

//                var sizedWideLargeImagePath = "ms-appdata:///local/liveTiles/" + sizedWideLargeImage.DisplayName;
//                var sizedSquareImagePath = "ms-appdata:///local/liveTiles/" + sizedSquareLargeImage.DisplayName;


//                tileXmlString = string.Format(tileFormat, sizedWideLargeImagePath, sizedSquareImagePath, text, sizedSquareImagePath);
//            }
//            //large image: TileWidePeekImageAndText01
//            else if (largeImageFile != null && largeImageIsWide)
//            {
//                var tileFormat = @"
//                <tile>
//                  <visual>
//                    <binding template='TileWidePeekImageAndText01'>
//                      <image id='1' src='{0}' alt='{1}'/>
//                      <text id='1'>{1}</text>
//                    </binding>
//                    <binding template='TileSquarePeekImageAndText04'>
//                      <image id='1' src='{2}' alt='{1}'/>
//                      <text id='1'>{1}</text>
//                    </binding>   
//                  </visual>
//                </tile>";

//                var sizedWideLargeImage = await _imagesService.GenerateResizedImage(largeImageFile, 310, 150, 0, 0, true) as StorageFile;
//                var sizedSquareLargeImage = await _imagesService.GenerateResizedImage(largeImageFile, 150, 150, 0, 0, true) as StorageFile;

//                var sizedWideLargeImagePath = "ms-appdata:///local/liveTiles/" + sizedWideLargeImage.DisplayName;
//                var sizedSquareImagePath = "ms-appdata:///local/liveTiles/" + sizedSquareLargeImage.DisplayName;

//                tileXmlString = string.Format(tileFormat, sizedWideLargeImagePath, text, sizedSquareImagePath);
//            }

//            //small image only text is long: TileWideSmallImageAndText03
//            else if (smallImageFile != null && textIsLong)
//            {
//                var tileFormat = @"
//                <tile>
//                  <visual>
//                    <binding template='TileWideSmallImageAndText03'>
//                      <image id='1' src='{0}' alt='{1}'/>
//                      <text id='1'>{1}</text>
//                    </binding>
//                    <binding template='TileSquarePeekImageAndText04'>
//                      <image id='1' src='{0}' alt='{1}'/>
//                      <text id='1'>{1}</text>
//                    </binding>
//                  </visual>
//                </tile>";

//                var smallImageFilePath = "ms-appdata:///local/liveTiles/" + smallImageFile.DisplayName + smallImageFile.FileType;

//                tileXmlString = string.Format(tileFormat, smallImageFilePath, text);
//            }

//            //small image only text is short: TileWideSmallImageAndText01
//            else if (smallImageFile != null && !textIsLong)
//            {
//                var tileFormat = @"
//                <tile>
//                  <visual>
//                    <binding template='TileWideSmallImageAndText01'>
//                      <image id='1' src='{0}' alt='{1}'/>
//                      <text id='1'>{1}</text>
//                    </binding>
//                    <binding template='TileSquarePeekImageAndText04'>
//                      <image id='1' src='{0}' alt='{1}'/>
//                      <text id='1'>{1}</text>
//                    </binding>
//                  </visual>
//                </tile>";

//                var smallImageFilePath = "ms-appdata:///local/liveTiles/" + smallImageFile.DisplayName + smallImageFile.FileType;

//                tileXmlString = string.Format(tileFormat, smallImageFilePath, text);
//            }

//            //no image, text is long: TileWideText04
//            else if (textIsLong)
//            {
//                var tileFormat = @"
//                <tile>
//                  <visual>
//                    <binding template='TileWideText04'>
//                      <text id='1'>{0}</text>
//                    </binding>  
//                    <binding template='TileSquareText04'>
//                        <text id='1'>{0}</text>
//                    </binding> 
//                  </visual>
//                </tile>";

//                tileXmlString = string.Format(tileFormat, text);
//            }

//            //no image text is short: TileWideText03
//            else
//            {
//                var tileFormat = @"
//                <tile>
//                  <visual>
//                    <binding template='TileWideText03'>
//                      <text id='1'>{0}</text>
//                    </binding>
//                    <binding template='TileSquareText04'>
//                        <text id='1'>{0}</text>
//                    </binding>  
//                  </visual>
//                </tile>";

//                tileXmlString = string.Format(tileFormat, text);
//            }
//            // Generate the final XML obj
//            XmlDocument final = new XmlDocument();
//            final.LoadXml(tileXmlString);

//            // Create and send the tile notification
//            TileNotification notification = new TileNotification(final);
//            _tileUpdater.Update(notification);
        }


        public void SetCount(int count)
        {
            lock (this)
            {
                List<object> liveTileImages = new List<object>();
                var settings = LoadTaskSettingsImpl();
                if (settings != null)
                {
                    liveTileImages.AddRange(settings.Value.tile_images);
                }

                UpdateLiveTile(liveTileImages, count, 0, 0);
            }
        }

        public void SetMessageRead(string id)
        {
            try
            {
                lock (this)
                {
                    if (!File.Exists(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "bgtaskMessages.txt"))
                    {
                        File.CreateText(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "bgtaskMessages.txt").Close();
                    }

                    using (var bgTaskToastedMessages = File.AppendText(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "bgtaskMessages.txt"))
                    {
                        bgTaskToastedMessages.WriteLine(id);
                    }
                }
            }
            catch { }
        }

        public IEnumerable<string> GetMessagesMarkedRead()
        {
            try
            {
                lock (this)
                {
                    HashSet<string> existingMessages = new HashSet<string>();
                    if (File.Exists(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "bgtaskMessages.txt"))
                    {
                        using (var bgTaskToastedMessages = File.OpenText(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "bgtaskMessages.txt"))
                        {
                            while (!bgTaskToastedMessages.EndOfStream)
                            {
                                var msgId = bgTaskToastedMessages.ReadLine();
                                if (!existingMessages.Contains(msgId))
                                    existingMessages.Add(msgId);
                            }
                        }

                    }
                    return existingMessages;
                }
            }
            catch { }
            return Enumerable.Empty<string>();
        }

        private static void UpdateLiveTile(List<object> tileImages, int messageCount, int liveTileCounter, int startTileCounter)
        {
            var activeTiles = ShellTile.ActiveTiles;
            var activeTile = activeTiles.FirstOrDefault();
            if (activeTile != null)
            {
                var uris = new List<Uri>();

                Utility.Shuffle(tileImages);

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

    }
}
