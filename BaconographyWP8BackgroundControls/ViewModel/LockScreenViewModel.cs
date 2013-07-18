using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace BaconographyWP8.ViewModel
{
    public class LockScreenViewModel
    {
        public LockScreenViewModel()
        {
            OverlayItems = new List<LockScreenMessage>();
        }

        public string ImageSource { get; set; }
        public List<LockScreenMessage> OverlayItems  { get; set; }
        public int NumberOfItems { get; set; }

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

                _displayText = _displayText.Replace("\r", " ").Replace("\n", " ").Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&apos;", "'").Trim();

                if (_displayText.Length > 100)
                    _displayText = _displayText.Substring(0, 100);
            }
        }
        public string Glyph { get; set; }
    }
}
