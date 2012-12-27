using BaconographyPortable.Messages;
using BaconographyPortable.Services;
using BaconographyPortable.ViewModel.Collections;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel
{
    public class FileOpenPickerViewModel : ViewModelBase
    {
        IUserService _userService;
        INavigationService _navigationService;
        IBaconProvider _baconProvider;

        public FileOpenPickerViewModel(IBaconProvider baconProvider)
        {
            _baconProvider = baconProvider;
            _navigationService = baconProvider.GetService<INavigationService>();
            _userService = baconProvider.GetService<IUserService>();
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
                RaisePropertyChanged("Query");
            }
        }

        private ImageSearchViewModelCollection _files;
        public ImageSearchViewModelCollection Files
        {
            get
            {
                return _files;
            }
            set
            {
                _files = value;
                RaisePropertyChanged("Files");
            }
        }

        private ImageViewModel _selectedFile;
        public ImageViewModel SelectedFile
        {
            get
            {
                return _selectedFile;
            }
            set
            {
                if (_selectedFile != null)
                    MessengerInstance.Send<PickerFileMessage>(new PickerFileMessage { TargetUrl = _selectedFile.Image, Selected = false });


                _selectedFile = value;
                RaisePropertyChanged("SelectedFile");
                MessengerInstance.Send<PickerFileMessage>(new PickerFileMessage { TargetUrl = _selectedFile.Image, Selected = true });
            }
        }

        static RelayCommand<FileOpenPickerViewModel> _search = new RelayCommand<FileOpenPickerViewModel>((vm) => vm.SearchImpl());
        public RelayCommand<FileOpenPickerViewModel> Search { get { return _search; } }

        private void SearchImpl()
        {
            Files = new ImageSearchViewModelCollection(_baconProvider, Query);
        }
    }
}
