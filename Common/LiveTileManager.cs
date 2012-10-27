using Newtonsoft.Json;
using Baconography.OfflineStore;
using Baconography.RedditAPI;
using Baconography.RedditAPI.Things;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Baconography.Common
{

	class LiveTileManager
	{
		static TileUpdater updater;
		static int tileCounter;

		static LiveTileManager()
		{
			updater = TileUpdateManager.CreateTileUpdaterForApplication();
			updater.EnableNotificationQueue(true);
			tileCounter = 0;
		}

		// Call prior to a set of MaybeCreateTile() calls.
		public static void StartUpdateSequence()
		{
			tileCounter = 0;
		}

		// Pass a LinkThing to MaybeCreateTile() to generate a live tile based on the link content.
		public static void MaybeCreateTile(Thing thing)
		{
			try
			{
				tileCounter++;
				if (tileCounter > 5)
					return;

				var linkThing = new TypedThing<Link>(thing);
				if (linkThing == null)
					return;

				Uri image = null;
				if (!String.IsNullOrEmpty(linkThing.Data.Thumbnail) && linkThing.Data.Thumbnail != "self" && linkThing.Data.Thumbnail != "default" && linkThing.Data.Thumbnail != "nsfw")
					image = new Uri(linkThing.Data.Thumbnail);

				CreateTile(linkThing.Data.Title, image, image);
			}
			catch (Exception)
			{
                tileCounter--;
			}
		}

		// Take a subreddit thing and generate a pinned secondary tile. Use the display
		// name and subreddit header image in the tile.
		public static async void CreateSecondaryTileForSubreddit(TypedThing<Subreddit> subreddit)
		{
			try
			{
				string id = "";
				if (subreddit != null)
					id = subreddit.Data.DisplayName;

				SecondaryTile tile = new SecondaryTile();
				tile.TileOptions = TileOptions.ShowNameOnWideLogo | TileOptions.ShowNameOnLogo;
				tile.DisplayName = "Baconography";
				tile.ShortName = "/r/" + id;
				if (subreddit != null)
				{

					// Download and create a local copy of the header image
					var rawImage = await Images.SaveFileFromUriAsync(new Uri(subreddit.Data.HeaderImage), subreddit.Data.DisplayName + ".jpg", "Images");
					// Generate a wide tile appropriate image
					var wideImage = await Images.GenerateResizedImageAsync(rawImage, 310, 150);
					// Generate a square tile appropriate image
					var squareImage = await Images.GenerateResizedImageAsync(rawImage, 150, 150);

					tile.WideLogo = new Uri("ms-appdata:///local/Images/" + wideImage.Name);
					tile.Logo = new Uri("ms-appdata:///local/Images/" + squareImage.Name);
					subreddit.Data.PublicDescription = null;
					subreddit.Data.Description = null;
					subreddit.Data.Headertitle = null;
					tile.Arguments = JsonConvert.SerializeObject(subreddit);
				}
				else
				{
					Uri logo = new Uri("ms-appx:///Assets/Logo.png");
					Uri wideLogo = new Uri("ms-appx:///Assets/WideLogo.png");
					tile.Arguments = "/r/";
					tile.Logo = logo;
					tile.WideLogo = wideLogo;
				}
				tile.TileId = "r" + id;

				// Ask the user to authorize creation of the tile
				bool isPinned = await tile.RequestCreateAsync();
			}
			catch (Exception)
			{
				// TODO: Do something with exceptions
			}
		}

		public static bool TileExists(string id)
		{
			return SecondaryTile.Exists("r" + id);
		}

		public static async void RemoveSecondaryTile(string id)
		{
			if (TileExists(id))
			{
				var tile = new SecondaryTile("r" + id);
				await tile.RequestDeleteAsync();
			}
		}

		// Generate a tile using the text provided (typically a link title). If an image is provided,
		// we use the correct WinRT template. Otherwise, just the text wrap template.
		static void CreateTile(string text, Uri squareImage = null, Uri wideImage = null)
		{
			StringBuilder builder = new StringBuilder(String.Empty);
			string wideTemplate = "TileWidePeekImageAndText01";
			if (wideImage == null)
			{
				wideTemplate = "TileWideText04";
			}

			string squareTemplate = "TileSquarePeekImageAndText04";
			if (squareTemplate == null)
			{
				squareTemplate = "TileSquareText04";
			}

			// A single tile can contain both square and wide formats, but we
			// have to go through extra work to add both bindings.
			string xmlImage = "<image id=\"1\" src=\"{0}\" />";
			string xmlText = "<text id=\"1\">{0}</text>";
			string xmlBinding = "<binding template=\"{0}\">{1}{2}</binding>";

			builder.Append("<tile><visual>");
			builder.AppendFormat(xmlBinding,
					wideTemplate,
					wideImage != null ? String.Format(xmlImage, wideImage.AbsoluteUri) : "",
					String.Format(xmlText, text));
			builder.AppendFormat(xmlBinding,
					squareTemplate,
					squareImage != null ? String.Format(xmlImage, wideImage.AbsoluteUri) : "",
					String.Format(xmlText, text));
			builder.Append("</visual></tile>");

			// Generate the final XML obj
			XmlDocument final = new XmlDocument();
			final.LoadXml(builder.ToString());

			// Create and send the tile notification
			TileNotification notification = new TileNotification(final);
			updater.Update(notification);
		}
	}
}
