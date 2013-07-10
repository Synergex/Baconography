using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel
{
    public class LinkedPictureViewModel : ViewModelBase
    {
        public class LinkedPicture : ViewModelBase
        {
			private object _imageSource;
            public object ImageSource
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
            public string Url { get; set; }
            public string Title { get; set; }
            public bool IsAlbum { get; set; }
            public int PositionInAlbum { get; set; }
            public int AlbumSize { get; set; }
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
                    var refiedValue = value.ToList();
                    if (refiedValue.Count > 1)
                    {
                        int i = 1;
                        foreach (var picture in refiedValue)
                        {
                            picture.IsAlbum = true;
                            picture.PositionInAlbum = i++;
                            picture.AlbumSize = refiedValue.Count;
                        }
                    }
                    else
                    {
                        foreach (var picture in refiedValue)
                        {
                            picture.IsAlbum = false;
                            picture.PositionInAlbum = 1;
                            picture.AlbumSize = 1;
                        }
                    }
                    _pictures = refiedValue;
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
