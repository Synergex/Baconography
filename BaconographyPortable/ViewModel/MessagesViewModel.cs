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
        ISmartOfflineService _smartOfflineService;
        INotificationService _notificationService;
        ILiveTileService _liveTileService;

        public MessagesViewModel(IBaconProvider baconProvider)
        {
            _baconProvider = baconProvider;
            _userService = baconProvider.GetService<IUserService>();
            _redditService = baconProvider.GetService<IRedditService>();
            _navigationService = baconProvider.GetService<INavigationService>();
            _dynamicViewLocator = baconProvider.GetService<IDynamicViewLocator>();
            _smartOfflineService = baconProvider.GetService<ISmartOfflineService>();
            _notificationService = baconProvider.GetService<INotificationService>();
            _liveTileService = baconProvider.GetService<ILiveTileService>();
            _smartOfflineService.OffliningOpportunity += _smartOfflineService_OffliningOpportunity;
            MessengerInstance.Register<UserLoggedInMessage>(this, UserLoggedIn);
        }

        DateTime _lastCheckedMessages = DateTime.Now;
        void _smartOfflineService_OffliningOpportunity(OffliningOpportunityPriority arg1, NetworkConnectivityStatus arg2, System.Threading.CancellationToken arg3)
        {
            if ((DateTime.Now - _lastCheckedMessages).TotalMinutes > 15)
            {
                _lastCheckedMessages = DateTime.Now;
                GetMessages();
            }
        }

        private async void GetMessages()
        {
            if (Messages == null)
            {
                Messages = new MessageViewModelCollection(_baconProvider);
                await Messages.LoadMoreItemsAsync(30);
            }
            else
            {
                Messages.Refresh();
            }

            lock (this)
            {
                _alreadyToastedMessages = new HashSet<string>(_liveTileService.GetMessagesMarkedRead());
            }
        }

        HashSet<string> _alreadyToastedMessages = new HashSet<string>();
        private void MaybeToastNewMessage(MessageViewModel viewModel)
        {
            lock (this)
            {
                if (!_alreadyToastedMessages.Contains(viewModel.Id))
                {
                    _alreadyToastedMessages.Add(viewModel.Id);
                    _liveTileService.SetMessageRead(viewModel.Id);
                    var newMessageCount = _messages.OfType<MessageViewModel>().Count(message => message.IsNew);
                    _liveTileService.SetCount(newMessageCount);
                    _notificationService.CreateNotificationWithNavigation("New Message: " + viewModel.Preview, _baconProvider.GetService<IDynamicViewLocator>().MessagesView, null);
                    HasMail = true;
                }
            }
        }

        private void UserLoggedIn(UserLoggedInMessage obj)
        {
            GetMessages();
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
                if (_selectedItem != null)
                {
                    var tempItem = _selectedItem;
                    // Mark the item as read
                    tempItem.IsNew = false;
                }

                if (value != null && value.IsNew)
                {
                    value.IsNew = false;
                    _redditService.ReadMessage(value.Name);
                    var newMessageCount = _messages.OfType<MessageViewModel>().Count(message => message.IsNew);
                    HasMail = newMessageCount > 0;
                    _liveTileService.SetCount(newMessageCount);
                }

                _selectedItem = value;
                RaisePropertyChanged("SelectedItem");
            }
        }

        bool _hasMail;
        public bool HasMail
        {
            get
            {
                return _hasMail;
            }
            set
            {
                _hasMail = value;
                RaisePropertyChanged("HasMail");
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
            vm.GetMessages();
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
