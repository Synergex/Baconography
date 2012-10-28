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
        }

        public IEnumerable<LinkedPicture> Pictures { get; set; }
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
    }
}
