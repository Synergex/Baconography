using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Baconography.Messages;
using Baconography.RedditAPI;
using Baconography.RedditAPI.Actions;
using Baconography.RedditAPI.Things;
using Baconography.Services;
using Baconography.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Baconography.OfflineStore;

namespace Baconography.ViewModel
{
    public class FileOpenPickerViewModel : ViewModelBase
    {
        private IUsersService _userService;
        private INavigationService _navigationService;

        public FileOpenPickerViewModel( INavigationService navigationService, IUsersService userService )
        {
            _navigationService = navigationService;
            _userService = userService;
        }

        private string _query;
        public string Query
        {
            get
            {
                return _query;
            }
            set
            {
                _query = value;
                RaisePropertyChanged( "Query" );
            }
        }
        
        private ObservableCollection<File> _files;
        public ObservableCollection<File> Files
        {
            get
            {
                return _files;
            }
            set
            {
                _files = value;
                RaisePropertyChanged( "Files" );
            }
        }

        private File _selectedFile;
        public File SelectedFile
        {
            get
            {
                return _selectedFile;
            }
            set
            {
                if(_selectedFile != null)
                    MessengerInstance.Send<PickerFileMessage>(new PickerFileMessage { TargetUrl = _selectedFile.Image, Selected = false });


                _selectedFile = value;
                RaisePropertyChanged( "SelectedFile" );
                MessengerInstance.Send<PickerFileMessage>(new PickerFileMessage { TargetUrl = _selectedFile.Image, Selected = true });
            }
        }

        private RelayCommand _search;
        public RelayCommand Search
        {
            get
            {
                if (_search == null)
                {
                    _search = new RelayCommand(
                        async () =>
                        {
                            Files = new ObservableCollection<File>();

                            var currentUser = await _userService.GetUser();
                            //limited to our known image sites for the user best experiance
                            var search = new Search { Query = Query + " AND (site:'imgur' OR site:'flickr' OR site:'memecrunch' OR site:'quickmeme' OR site:qkme OR site:'min' OR site:'picsarus'" };
                            var searchListing = await search.Run( currentUser );
                            
                            foreach ( Thing thing in searchListing.Data.Children )
                            {
                                var linkData = thing.Data as Link;
                                if(linkData == null || linkData.Url == null)
                                    continue;

                                var images = await Images.GetImagesFromUrl(linkData.Title, linkData.Url);

                                foreach (var image in images)
                                {
                                    Files.Add(new File { Image = image.Item2, Title = image.Item1, Description = ""});
                                }
                            }
                        });
                }
                return _search;
            }
        }

        public class File
        {
            public string Image         { get; set; }
            public string Title         { get; set; }
            public string Description   { get; set; }
        }
    }
}
