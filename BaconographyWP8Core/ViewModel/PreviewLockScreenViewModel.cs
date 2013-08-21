using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
using BaconographyWP8;
using BaconographyWP8.Common;
using BaconographyWP8.Converters;
using BaconographyWP8.ViewModel;
using GalaSoft.MvvmLight;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BaconographyWP8Core.ViewModel
{
    public class PreviewLockScreenViewModel : ViewModelBase
    {

        public PreviewLockScreenViewModel()
        {
            var locator = Application.Current.Resources["Locator"] as ViewModelLocator;
            var settingsService = ServiceLocator.Current.GetInstance<ISettingsService>();
            if (locator != null)
            {
                var lsvm = locator.LockScreen;
                this.ImageSource = lsvm.ImageSource;
                this.NumberOfItems = lsvm.NumberOfItems;
                this.OverlayItems = lsvm.OverlayItems;
                this.OverlayOpacity = lsvm.OverlayOpacity;
                this.RoundedCorners = lsvm.RoundedCorners;
                this.ShowMessages = settingsService.MessagesInLockScreenOverlay;
                this.ShowTopPosts = settingsService.PostsInLockScreenOverlay;

                if (_overlayItems.Count == 0 ||
                    (_overlayItems.Count > 0 && _overlayItems.First().Glyph != Utility.UnreadMailGlyph))
                {
                    this._overlayItems.Insert(0, new LockScreenMessage { Glyph = Utility.UnreadMailGlyph, DisplayText = "Sample unread message" });
                }

                var SampleCollection = new List<LockScreenMessage> {
                    new LockScreenMessage { Glyph = LinkGlyphConverter.PhotoGlyph, DisplayText = "The funniest picture on the front page" },
                    new LockScreenMessage { Glyph = LinkGlyphConverter.WebGlyph, DisplayText = "Very interesting article about cats" },
                    new LockScreenMessage { Glyph = LinkGlyphConverter.DetailsGlyph, DisplayText = "I am the walrus, AMA" },
                    new LockScreenMessage { Glyph = LinkGlyphConverter.VideoGlyph, DisplayText = "I am proud to present a short film about film critics" },
                    new LockScreenMessage { Glyph = LinkGlyphConverter.MultiredditGlyph, DisplayText = "A multireddit of all of the best stuff that reddit has to offer" },
                    new LockScreenMessage { Glyph = LinkGlyphConverter.PhotoGlyph, DisplayText = "Breathtaking vista of a massive canyon" }
                };

                this._overlayItems.AddRange(SampleCollection);
            }
        }

        string _imageSource;
        public string ImageSource
        {
            get
            {
                return _imageSource;
            }
            set
            {
                _imageSource = value;
                RaisePropertyChanged("ImageSource");
            }
        }

        List<LockScreenMessage> _overlayItems;
        public List<LockScreenMessage> OverlayItems
        {
            get
            {
                List<LockScreenMessage> collection = new List<LockScreenMessage>();
                if (ShowMessages)
                    collection.AddRange(_overlayItems.Where(p => p.Glyph == Utility.UnreadMailGlyph));
                if (ShowTopPosts)
                    collection.AddRange(_overlayItems.Where(p => p.Glyph != Utility.UnreadMailGlyph));

                return collection.Take(NumberOfItems).ToList();
            }
            set
            {
                _overlayItems = value;
                RaisePropertyChanged("OverlayItems");
            }
        }

        int _numberOfItems;
        public int NumberOfItems
        {
            get
            {
                return _numberOfItems;
            }
            set
            {
                _numberOfItems = value;
                RaisePropertyChanged("NumberOfItems");
                RaisePropertyChanged("OverlayItems");
            }
        }

        bool _showMessages;
        public bool ShowMessages
        {
            get
            {
                return _showMessages;
            }
            set
            {
                _showMessages = value;
                RaisePropertyChanged("OverlayItems");
            }
        }

        bool _showTopPosts;
        public bool ShowTopPosts
        {
            get
            {
                return _showTopPosts;
            }
            set
            {
                _showTopPosts = value;
                RaisePropertyChanged("OverlayItems");
            }
        }

        bool _roundedCorners;
        public bool RoundedCorners
        {
            get
            {
                return _roundedCorners;
            }
            set
            {
                _roundedCorners = value;
                RaisePropertyChanged("CornerRadius");
                RaisePropertyChanged("Margin");
                RaisePropertyChanged("InnerMargin");
            }
        }

        public CornerRadius CornerRadius
        {
            get
            {
                if (RoundedCorners)
                    return new CornerRadius(5);
                return new CornerRadius(0);
            }
        }

        public Thickness Margin
        {
            get
            {
                if (RoundedCorners)
                    return new Thickness(12, 40, 12, 12);
                return new Thickness(-5, 40, -5, 0);
            }
        }

        public Thickness InnerMargin
        {
            get
            {
                if (RoundedCorners)
                    return new Thickness(0, 0, 0, 0);
                return new Thickness(17, 0, 17, 0);
            }
        }

        float _overlayOpacity;
        public float OverlayOpacity
        {
            get
            {
                return _overlayOpacity;
            }
            set
            {
                if (value > 1)
                    _overlayOpacity = value / 100;
                else
                    _overlayOpacity = value;
                RaisePropertyChanged("OverlayOpacity");
            }
        }
    }
}
