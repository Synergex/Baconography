using BaconographyPortable.Common;
using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
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
    public class MessagesViewModel : ViewModelBase
    {
        IBaconProvider _baconProvider;
        IUserService _userService;
        IRedditService _redditService;
        INavigationService _navigationService;
        IDynamicViewLocator _dynamicViewLocator;
        TypedThing<Link> _linkThing;

        public MessagesViewModel(IBaconProvider baconProvider)
        {
            _baconProvider = baconProvider;
            _userService = baconProvider.GetService<IUserService>();
            _redditService = baconProvider.GetService<IRedditService>();
            _navigationService = baconProvider.GetService<INavigationService>();
            _dynamicViewLocator = baconProvider.GetService<IDynamicViewLocator>();

        }

        ComposeViewModel _composeVM;
        public ComposeViewModel Compose
        {
            get
            {
                return _composeVM;
            }
            set
            {
                _composeVM = value;
                RaisePropertyChanged("Compose");
            }
        }

        MessageViewModel _selectedItem;
        public MessageViewModel SelectedItem
        {
            get
            {
                return _selectedItem;
            }
            set
            {
                _selectedItem = value;
                RaisePropertyChanged("SelectedItem");
            }
        }

        public MessageViewModelCollection Messages { get; private set; }
        public ObservableCollection<ViewModelBase> Unread
        {
            get
            {
                return Messages.UnreadMessages;
            }
        }

        public RelayCommand<MessagesViewModel> RefreshMessages { get { return _refreshMessages; } }
        static RelayCommand<MessagesViewModel> _refreshMessages = new RelayCommand<MessagesViewModel>(RefreshMessagesImpl);
        private static void RefreshMessagesImpl(MessagesViewModel vm)
        {
            vm.Messages = new MessageViewModelCollection(vm._baconProvider);
        }

        public RelayCommand<MessagesViewModel> NewMessage { get { return _newMessage; } }
        static RelayCommand<MessagesViewModel> _newMessage = new RelayCommand<MessagesViewModel>(NewMessageImpl);
        private static void NewMessageImpl(MessagesViewModel vm)
        {
            vm._composeVM = new ComposeViewModel(vm._baconProvider);
            vm._navigationService.Navigate(vm._dynamicViewLocator.ComposeView, null);
        }

        public RelayCommand<MessagesViewModel> ReplyToMessage { get { return _replyToMessage; } }
        static RelayCommand<MessagesViewModel> _replyToMessage = new RelayCommand<MessagesViewModel>(ReplyToMessageImpl);
        private static void ReplyToMessageImpl(MessagesViewModel vm)
        {
            vm._composeVM = new ComposeViewModel(vm._baconProvider, vm.SelectedItem);
            vm._navigationService.Navigate(vm._dynamicViewLocator.ComposeView, null);
        }
    }
}
