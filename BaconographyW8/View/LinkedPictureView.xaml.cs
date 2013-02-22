﻿using BaconographyPortable.ViewModel;
using DXRenderInterop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace BaconographyW8.View
{
    /// <summary>
    /// A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class LinkedPictureView : BaconographyW8.Common.LayoutAwarePage
    {
        //cheating a little bit here but its for the best
        LinkedPictureViewModel _pictureViewModel;
        IEnumerable<Tuple<string, string>> _navData;
        public LinkedPictureView()
        {
            this.InitializeComponent();
        }

        private async Task<byte[]> DownloadImageFromWebsiteAsync(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                using (WebResponse response = await request.GetResponseAsync())
                {
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
                return null;
            }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">The parameter value passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested.
        /// </param>
        /// <param name="pageState">A dictionary of state preserved by this page during an earlier
        /// session.  This will be null the first time a page is visited.</param>
        protected override async void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            var pictureData = navigationParameter as IEnumerable<Tuple<string, string>>;

            if (pictureData == null && pageState != null && pageState.ContainsKey("NavagationData"))
            {
                _navData = pictureData = pageState["NavagationData"] as IEnumerable<Tuple<string, string>>;
            }

            if (pictureData != null)
            {
                _navData = pictureData;
                var pictureTasks = pictureData.Select(async (tpl) =>
                {
                    var renderer = GifRenderer.CreateGifRenderer(await DownloadImageFromWebsiteAsync(tpl.Item2));
                    if (renderer != null)
                    {
                        renderer.Visible = true;
                        return new LinkedPictureViewModel.LinkedPicture { Title = tpl.Item1, ImageSource = renderer };
                    }
                    else
                        return new LinkedPictureViewModel.LinkedPicture { Title = tpl.Item1, ImageSource = tpl.Item2 };
                })
                .ToArray();

                _pictureViewModel = new LinkedPictureViewModel 
                {
                    Pictures = await Task.WhenAll(pictureTasks)
                };
            }

            DataContext = _pictureViewModel;
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
            pageState["PictureViewModel"] = _navData;
            if (_pictureViewModel != null)
            {
                foreach (var linkedPicture in _pictureViewModel.Pictures)
                {
                    if (linkedPicture.ImageSource is GifRenderer)
                    {
                        ((GifRenderer)linkedPicture.ImageSource).Visible = false;
                    }
                }
            }
        }
    }
}
