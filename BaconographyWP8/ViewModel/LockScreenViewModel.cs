using BaconographyPortable.Messages;
using BaconographyPortable.Services;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyWP8.ViewModel
{
    public class LockScreenViewModel
    {
        public LockScreenViewModel()
        {
            Messenger.Default.Register<SettingsChangedMessage>(this, settingsChanged);
        }

        private void settingsChanged(SettingsChangedMessage obj)
        {
            var settingsService = ServiceLocator.Current.GetInstance<ISettingsService>();
            OverlayOpacity = settingsService.OverlayOpacity;
        }

        public string ImageSource { get; set; }
        public IEnumerable<LockScreenMessage> OverlayItems  { get; set; }

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
            }
        }
    }

    public class LockScreenMessage
    {
        string _displayText;
        public string DisplayText
        {
            get
            {
                return _displayText;
            }
            set
            {
                _displayText = value;

                _displayText = _displayText.Replace("\r", " ").Replace("\n", " ");

                if (_displayText.Length > 100)
                    _displayText = _displayText.Substring(0, 100);

                _displayText = _displayText.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&apos;", "'").Trim();
            }
        }
        public string Glyph { get; set; }
    }
}
