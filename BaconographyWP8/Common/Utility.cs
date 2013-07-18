using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using BaconographyWP8.Converters;
using BaconographyWP8.PlatformServices;
using BaconographyWP8.ViewModel;
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

        public static async Task<LockScreenViewModel> MakeLockScreenControl(ISettingsService settingsService, IRedditService redditService, IUserService userService, IImagesService imagesService, IBaconProvider baconProvider)
        {
            var user = (await userService.GetUser());
            if (user.Me != null && user.Me.HasMail)
            {
                //toast the user that they have mail
            }
            //call for posts from front page
            var frontPageResult = await redditService.GetPostsBySubreddit("/", 3);
            LinkGlyphConverter linkGlyphConverter = new LinkGlyphConverter();
            List<LockScreenMessage> lockScreenMessages = new List<LockScreenMessage>(frontPageResult.Data.Children.Select(thing => new LockScreenMessage { DisplayText = ((Link)thing.Data).Title, Glyph = linkGlyphConverter != null ? (string)linkGlyphConverter.Convert(((Link)thing.Data), typeof(String), null, System.Globalization.CultureInfo.CurrentCulture) : "" }));
            //maybe call for messages from logged in user
            if (user != null && user.LoginCookie != null)
            {

                var messages = await redditService.GetMessages(5);
                lockScreenMessages.AddRange(messages.Data.Children.Take(3).Select(thing => new LockScreenMessage
                {
                    DisplayText = thing.Data is CommentMessage ? ((CommentMessage)thing.Data).LinkTitle : ((Message)thing.Data).Subject,
                    Glyph = ((Message)thing.Data).New ? UnreadMailGlyph : ReadMailGlyph
                }));
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
                BitmapImage imageSource = null;
                for (int i = 0; i < imagesLinks.Count; i++)
                {
                    try
                    {

                        var url = ((Link)imagesLinks[i].Data).Url;
                        imageSource = new BitmapImage();
                        imageSource.CreateOptions = BitmapCreateOptions.None;

                        using (var stream = await ImagesService.ImageStreamFromUrl(url))
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

                        using (var theFile = File.Create(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\lockScreenCache.jpg"))
                        {
                            bitmap.SaveJpeg(theFile, 480, 800, 0, 100);
                            theFile.Flush(true);
                            theFile.Close();
                        }
                        break;
                    }
                    catch
                    {
                        continue;
                    }

                    break;
                }
                var vml = new LockScreenViewModel();
                vml.ImageSource = "lockScreenCache.jpg";
                vml.OverlayItems = lockScreenMessages;
                vml.OverlayOpacity = settingsService.OverlayOpacity;
                return vml;
            }
            return null;
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
