using Baconography.PlatformServices.ImageAPI;
using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Media;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;

namespace BaconographyWP8.PlatformServices
{
    public class ImagesService : IImagesService
    {
        private static async Task<StorageFile> SaveFileFromUriAsync(Uri fileUri, string localFileName, string localPath = "Images", NameCollisionOption collisionOption = NameCollisionOption.ReplaceExisting)
        {
            if (localFileName.StartsWith("/"))
                localFileName = localFileName.Substring(1);
            var file = await StorageFile.CreateStreamedFileFromUriAsync(localFileName, fileUri, Windows.Storage.Streams.RandomAccessStreamReference.CreateFromUri(fileUri));
            var destinationFolder = await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync(localPath, CreationCollisionOption.OpenIfExists);
            var outFile = await file.CopyAsync(destinationFolder, file.Name, collisionOption);
            return outFile;
        }

        public async Task<object> SaveFileFromUriAsync(Uri fileUri, string localFileName, string localPath = "Images", bool replaceIfExists = true)
        {
            return await SaveFileFromUriAsync(fileUri, localFileName, localPath, replaceIfExists ? NameCollisionOption.ReplaceExisting : NameCollisionOption.FailIfExists); 
        }

        public Task<byte[]> ImageBytesFromUrl(string url)
        {
            return ImageBytesFromUrl(url, false);
        }

        public static async Task<Stream> ImageStreamFromUrl(string url)
        {
            var uri = new Uri(url);

            string filename = Path.GetFileName(uri.LocalPath);

            if (filename.EndsWith(".jpg") || filename.EndsWith(".png") || filename.EndsWith(".jpeg") || filename.EndsWith(".gif"))
            {
            }
            else
            {
                var targetHost = uri.DnsSafeHost.ToLower(); //make sure we can compare caseless
                IEnumerable<Tuple<string, string>> imageAPIResults = null;
                switch (targetHost)
                {
                    case "imgur.com":
                        imageAPIResults = await Imgur.GetImagesFromUri("", uri);
                        break;
                    case "min.us":
                        imageAPIResults = await Minus.GetImagesFromUri("", uri);
                        break;
                    case "www.quickmeme.com":
                    case "i.qkme.me":
                    case "quickmeme.com":
                    case "qkme.me":
                        imageAPIResults = Quickmeme.GetImagesFromUri("", uri);
                        break;
                    case "memecrunch.com":
                        imageAPIResults = Memecrunch.GetImagesFromUri("", uri);
                        break;
                    case "www.flickr.com":
                    case "flickr.com":
                        imageAPIResults = await Flickr.GetImagesFromUri("", uri);
                        break;
                }

                if (imageAPIResults != null)
                {
                    uri = new Uri(imageAPIResults.First().Item2);
                    filename = Path.GetFileName(uri.LocalPath);
                }
            }


            if (!(filename.EndsWith(".jpg") || filename.EndsWith(".png") || filename.EndsWith(".jpeg") || filename.EndsWith(".gif")))
                return null;

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.AllowReadStreamBuffering = true;
            using (WebResponse response = await SimpleHttpService.GetResponseAsync(request))
            {
               return response.GetResponseStream();
            }
        }

        public async Task<byte[]> ImageBytesFromUrl(string url, bool isRetry)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                using (WebResponse response = await SimpleHttpService.GetResponseAsync(request))
                {
                    if (response == null)
                        return null;

                    using (Stream imageStream = response.GetResponseStream())
                    {
                        using (var result = new MemoryStream())
                        {
                            await imageStream.CopyToAsync(result);
                            return result.ToArray();
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                if (isRetry || !System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                    return null;
            }

            //delay a bit and try again
            await Task.Delay(500);
            return await ImageBytesFromUrl(url, true);
        }

        private static byte[] GenerateTransparentBitmap(uint width, uint height)
        {
            var bytes = new List<byte>();

            for (int column = 0; column < height; column++)
            {
                for (int row = 0; row < width; row++)
                {
                    bytes.Add(Colors.Transparent.B);
                    bytes.Add(Colors.Transparent.G);
                    bytes.Add(Colors.Transparent.R);
                    bytes.Add(Colors.Transparent.A);
                }
            }

            return bytes.ToArray();
        }

        private static byte[] MergePixelArrays(byte[] largeArray, uint lWidth, uint lHeight, byte[] smallArray, uint sWidth, uint sHeight, uint widthOffset, uint heightOffset)
        {

            for (uint rows = heightOffset, sRows = 0; sRows < sHeight; rows++, sRows++)
            {
                for (uint cols = widthOffset, sCols = 0; sCols < sWidth; cols++, sCols++)
                {
                    largeArray[(lWidth * rows + cols) * 4 + 0] = smallArray[(sWidth * sRows + sCols) * 4 + 0];
                    largeArray[(lWidth * rows + cols) * 4 + 1] = smallArray[(sWidth * sRows + sCols) * 4 + 1];
                    largeArray[(lWidth * rows + cols) * 4 + 2] = smallArray[(sWidth * sRows + sCols) * 4 + 2];
                    largeArray[(lWidth * rows + cols) * 4 + 3] = smallArray[(sWidth * sRows + sCols) * 4 + 3];
                }
            }

            return largeArray;
        }

        private static async Task<StorageFile> GenerateResizedImageAsync(StorageFile inputFile, uint width, uint height, uint edgePadding = 5, uint bottomPadding = 20, NameCollisionOption collisionOption = NameCollisionOption.ReplaceExisting)
        {
            //try
            //{
            //    string fileName = inputFile.DisplayName + width + "x" + height;
            //    string extension = inputFile.Name.Substring(inputFile.Name.LastIndexOf('.'));

            //    string folder = inputFile.Path.Substring(0, inputFile.Path.LastIndexOf('\\'));
            //    var outputFolder = await StorageFolder.GetFolderFromPathAsync(folder);
            //    var newFile = await outputFolder.CreateFileAsync(fileName + extension, CreationCollisionOption.ReplaceExisting);

            //    var inputStream = await inputFile.OpenAsync(Windows.Storage.FileAccessMode.Read);
            //    var outputStream = await newFile.OpenTransactedWriteAsync();

            //    var inMemStream = new InMemoryRandomAccessStream();
            //    var decoder = await BitmapDecoder.CreateAsync(inputStream);
            //    var encoder = await BitmapEncoder.CreateForTranscodingAsync(inMemStream, decoder);

            //    // Find aspect ratio for resize
            //    float nPercentW = (((float)width - (edgePadding * 2)) / (float)decoder.PixelWidth);
            //    float nPercentH = (((float)height - (edgePadding * 2)) / (float)decoder.PixelHeight);
            //    float nPercent = nPercentH < nPercentW ? nPercentH : nPercentW;

            //    // Scale height and width
            //    if (nPercent < 1)
            //    {
            //        encoder.BitmapTransform.ScaledHeight = (uint)(decoder.PixelHeight * nPercent);
            //        encoder.BitmapTransform.ScaledWidth = (uint)(decoder.PixelWidth * nPercent);
            //    }

            //    // Image may still exceed intended bounds, resize as appropriate
            //    if (encoder.BitmapTransform.ScaledWidth > width || encoder.BitmapTransform.ScaledHeight > height)
            //    {
            //        BitmapBounds bounds = new BitmapBounds();
            //        if (encoder.BitmapTransform.ScaledWidth > width)
            //        {
            //            bounds.Width = width;
            //            bounds.X = (encoder.BitmapTransform.ScaledWidth - width) / 2;
            //        }
            //        else
            //            bounds.Width = encoder.BitmapTransform.ScaledWidth;
            //        if (encoder.BitmapTransform.ScaledHeight > height)
            //        {
            //            bounds.Height = height;
            //            bounds.Y = (encoder.BitmapTransform.ScaledHeight - height) / 2;
            //        }
            //        else
            //            bounds.Height = encoder.BitmapTransform.ScaledHeight;
            //        encoder.BitmapTransform.Bounds = bounds;
            //    }
            //    await encoder.FlushAsync();

            //    var outDecoder = await BitmapDecoder.CreateAsync(inMemStream);
            //    var outEncoder = await BitmapEncoder.CreateForTranscodingAsync(outputStream.Stream, outDecoder);

            //    var transparentBytes = GenerateTransparentBitmap(width, height);

            //    PixelDataProvider data = await outDecoder.GetPixelDataAsync();
            //    uint heightOffset = (height - outDecoder.PixelHeight) / 2 - bottomPadding;
            //    uint widthOffset = (width - outDecoder.PixelWidth) / 2;
            //    byte[] bytes = MergePixelArrays(transparentBytes, width, height, data.DetachPixelData(), outDecoder.PixelWidth, outDecoder.PixelHeight, widthOffset, heightOffset);
            //    outEncoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, width, height, 72.0, 72.0, bytes);

            //    await outEncoder.FlushAsync();
            //    return newFile;
            //}
            //catch (Exception)
            //{

            //}

            return null;
        }

        public async Task<IEnumerable<Tuple<string, string>>> GetImagesFromUrl(string title, string url)
        {
            try
            {
                var uri = new Uri(url);

                string filename = Path.GetFileName(uri.LocalPath);

                if (filename.EndsWith(".jpg") || filename.EndsWith(".png") || filename.EndsWith(".jpeg") || filename.EndsWith(".gif"))
                    return new Tuple<string, string>[] { Tuple.Create(title, url) };
                else
                {
                    var targetHost = uri.DnsSafeHost.ToLower(); //make sure we can compare caseless

                    switch (targetHost)
                    {
                        case "imgur.com":
                            return await Imgur.GetImagesFromUri(title, uri);
                        case "min.us":
                            return await Minus.GetImagesFromUri(title, uri);
                        case "www.quickmeme.com":
                        case "i.qkme.me":
                        case "quickmeme.com":
                        case "qkme.me":
                            return Quickmeme.GetImagesFromUri(title, uri);
                        case "memecrunch.com":
                            return Memecrunch.GetImagesFromUri(title, uri);
                        case "flickr.com":
                            return await Flickr.GetImagesFromUri(title, uri);
                        default:
                            return Enumerable.Empty<Tuple<string, string>>();
                    }
                }
            }
            catch
            {
                return Enumerable.Empty<Tuple<string, string>>();
            }
        }

        public async  Task<object> GenerateResizedImage(object inputFile, uint width, uint height, uint edgePadding = 5, uint bottomPadding = 20, bool replaceIfExists = true)
        {
            return await GenerateResizedImageAsync(inputFile as StorageFile, width, height, edgePadding, bottomPadding, replaceIfExists ? NameCollisionOption.ReplaceExisting : NameCollisionOption.FailIfExists);
        }
    

        public bool MightHaveImagesFromUrl(string url)
        {
 	         try
            {
                var uri = new Uri(url);

                string filename = Path.GetFileName(uri.LocalPath);

                if (filename.EndsWith(".jpg") || url.EndsWith(".png") || url.EndsWith(".jpeg") || filename.EndsWith(".gif"))
                    return true;
                else
                {
                    var targetHost = uri.DnsSafeHost.ToLower(); //make sure we can compare caseless

                    switch (targetHost)
                    {
                        case "imgur.com":
                        case "min.us":
                        case "www.quickmeme.com":
                        case "i.qkme.me":
                        case "quickmeme.com":
                        case "qkme.me":
                        case "memecrunch.com":
                        case "flickr.com":
                            return true;
                    }
                }

            }
            catch
            {
                //ignore failure here, we're going to return false anyway
            }
            return false;
        }


        public bool IsImage(string url)
        {
            try
            {
                var uri = new Uri(url);

                string filename = Path.GetFileName(uri.LocalPath);

                if (filename.EndsWith(".jpg") || url.EndsWith(".png") || url.EndsWith(".jpeg") || filename.EndsWith(".gif"))
                    return true;
            }
            catch
            {
                //ignore failure here, we're going to return false anyway
            }
            return false;
        }

        public bool IsImageAPI(string url)
        {
            try
            {
                var uri = new Uri(url);

                string filename = Path.GetFileName(uri.LocalPath);

                if (filename.EndsWith(".jpg") || url.EndsWith(".png") || url.EndsWith(".jpeg") || filename.EndsWith(".gif"))
                    return false;
                else
                {
                    var targetHost = uri.DnsSafeHost.ToLower(); //make sure we can compare caseless

                    switch (targetHost)
                    {
                        case "imgur.com":
                            return Imgur.IsAPI(uri);
                        case "min.us":
                            return Minus.IsAPI(uri);
                        case "flickr.com":
                            return Flickr.IsAPI(uri);
                        case "www.quickmeme.com":
                        case "i.qkme.me":
                        case "quickmeme.com":
                        case "qkme.me":
                        case "memecrunch.com":
                            return false;
                    }
                }

            }
            catch
            {
                //ignore failure here, we're going to return false anyway
            }
            return false;
        }

    }
}