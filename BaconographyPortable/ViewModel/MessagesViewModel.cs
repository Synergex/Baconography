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

        public MessagesViewModel(IBaconProvider baconProvider)
        {
            _baconProvider = baconProvider;
            _userService = baconProvider.GetService<IUserService>();
            _redditService = baconProvider.GetService<IRedditService>();
            _navigationService = baconProvider.GetService<INavigationService>();
            _dynamicViewLocator = baconProvider.GetService<IDynamicViewLocator>();
            MessengerInstance.Register<UserLoggedInMessage>(this, UserLoggedIn);
        }

        private void UserLoggedIn(UserLoggedInMessage obj)
        {
            if (Messages == null)
                Messages = new MessageViewModelCollection(_baconProvider);
            else
                Messages.Refresh();
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
                // If the user deselects a "new" item
                if (_selectedItem != null && _selectedItem.IsNew)
                {
                    var tempItem = _selectedItem;
                    // Mark the item as read
                    tempItem.IsNew = false;
                    _redditService.ReadMessage(tempItem.Name);

                    // Reinsert the item into the collection (to cause unread to update)
                    int index = Messages.IndexOf(tempItem);
                    Messages.RemoveAt(index);
                    Messages.Insert(index, tempItem);
                }

                _selectedItem = value;
                RaisePropertyChanged("SelectedItem");
            }
        }

        public bool HasMail
        {
            get
            {
                return Messages.Any(message => ((MessageViewModel)message).IsNew);
            }
        }

        MessageViewModelCollection _messages;
        public MessageViewModelCollection Messages
        {
            get
            {
                return _messages;
            }
            set
            {
                _messages = value;
                RaisePropertyChanged("Messages");
            }
        }

        public RelayCommand<MessagesViewModel> RefreshMessages { get { return _refreshMessages; } }
        static RelayCommand<MessagesViewModel> _refreshMessages = new RelayCommand<MessagesViewModel>(RefreshMessagesImpl);
        private static void RefreshMessagesImpl(MessagesViewModel vm)
        {
            vm.Messages.Refresh();
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
            if (vm.SelectedItem.IsPostReply || vm.SelectedItem.IsCommentReply || vm.SelectedItem.IsUserMention)
            {
                UtilityCommandImpl.GotoLinkImpl("http://reddit.com" + vm.SelectedItem.Context, null);
            }
            else
            {
                vm._composeVM = new ComposeViewModel(vm._baconProvider, vm.SelectedItem);
                vm._navigationService.Navigate(vm._dynamicViewLocator.ComposeView, null);
            }
        }
    }
}
