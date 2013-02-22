using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel
{
    public class WebVideoViewModel : ViewModelBase
    {
        public WebVideoViewModel(IEnumerable<Dictionary<string, string>> avalableStreams)
        {
            _availableStreams = avalableStreams;
            _url = avalableStreams.First()["url"];
            _selectedStream = AvailableStreams.First();
        }

        private string _url;
        public string Url
        {
            get
            {
                return _url;
            }
        }
        IEnumerable<Dictionary<string, string>> _availableStreams;
        public IEnumerable<string> AvailableStreams
        {
            get
            {
                return _availableStreams.Select(stream => CleanName(stream["type"]) + " : " + stream["quality"]);
            }
        }

        string _selectedStream;
        public string SelectedStream
        {
            get
            {
                return _selectedStream;
            }
            set
            {
                if (_selectedStream != value)
                {
                    _selectedStream = value;

                    var selectedStream = _availableStreams.FirstOrDefault(stream => (CleanName(stream["type"]) + " : " + stream["quality"]) == _selectedStream);
                    if (selectedStream != null)
                    {
                        _url = selectedStream["url"];
                        RaisePropertyChanged("Url");
                    }
                }

            }
        }
        private static string CleanName(string dirtyName)
        {
            switch (dirtyName)
            {
                case "video/webm;+codecs":
                    return "webm";
                case "video/mp4;+codecs":
                    return "mp4";
                case "video/flv":
                    return "flash";
                case "video/3gpp":
                    return "mobile";
                default:
                    return "unknown";
            }
        }
    }
}
