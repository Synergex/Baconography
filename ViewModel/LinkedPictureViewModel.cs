using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.ViewModel
{
    public class LinkedPictureViewModel : ViewModelBase
    {
        public class LinkedPicture
        {
            public string Url { get; set; }
            public string Title { get; set; }
            public bool IsAlbum { get; set; }
        }

        public IEnumerable<LinkedPicture> _pictures;
        public IEnumerable<LinkedPicture> Pictures
        {
            get
            {
                return _pictures;
            }
            set
            {
                if (value != null)
                {
                    if (value.Count() > 1)
                    {
                        foreach (var picture in value)
                        {
                            picture.IsAlbum = true;
                        }
                    }
                    else
                    {
                        foreach (var picture in value)
                        {
                            picture.IsAlbum = false;
                        }
                    }
                    _pictures = value;
                }
                else
                    _pictures = null;
            }
        }
        public string ImageTitle
        {
            get
            {
                var firstPicture = Pictures.FirstOrDefault();
                if (firstPicture != null)
                    return firstPicture.Title;
                else
                    return "";
            }
        }

        public bool IsAlbum
        {
            get
            {
                return Pictures != null && Pictures.Count() > 1;
            }
        }
    }
}
