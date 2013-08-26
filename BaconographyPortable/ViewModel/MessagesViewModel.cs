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
                Messages.CollectionChanged += (sender, args) => BridgeChange(_unreadMessages, args);
                await Messages.LoadMoreItemsAsync(30);
            }
            else
            {
                _unreadMessages.Clear();
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
                    _liveTileService.SetCount(_unreadMessages.Count);
                    _notificationService.CreateNotification("New Message: " + viewModel.Preview);
                    HasMail = true;
                }
            }
        }

        private void BridgeChange(ObservableCollection<MessageViewModel> target, System.Collections.Specialized.NotifyCollectionChangedEventArgs args)
        {
            switch (args.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    if (args.NewItems[0] is MessageViewModel && (args.NewItems[0] as MessageViewModel).IsNew)
                    {
                        var result = args.NewItems[0] as MessageViewModel;
                        target.Add(result);
                        MaybeToastNewMessage(result);
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    if (target.Contains(args.OldItems[0] as ViewModelBase))
                        target.Remove(args.OldItems[0] as MessageViewModel);

                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    var targetIndex = target.IndexOf(args.OldItems[0] as MessageViewModel);
                    if (targetIndex != -1)
                    {
                        if (args.NewItems[0] is MessageViewModel && ((MessageViewModel)args.NewItems[0]).IsNew)
                        {
                            target[targetIndex] = args.NewItems[0] as MessageViewModel;
                        }
                        else
                        {
                            target.RemoveAt(targetIndex);
                        }
                    }
                    else
                    {
                        if (args.NewItems[0] is MessageViewModel && ((MessageViewModel)args.NewItems[0]).IsNew)
                        {
                            var result = args.NewItems[0] as MessageViewModel;
                            target.Add(result);
                            MaybeToastNewMessage(result);
                        }
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    target.Clear();
                    break;
                default:
                    break;
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
                    _unreadMessages.Remove(value);
                }

                if (value != null && value.IsNew)
                {
                    value.IsNew = false;
                    _redditService.ReadMessage(value.Name);
                    _unreadMessages.Remove(value);
                    var newMessageCount = _messages.OfType<MessageViewModel>().Count(message => message.IsNew);
                    HasMail = newMessageCount > 0;
                    _liveTileService.SetCount(newMessageCount);
                }

                _selectedItem = value;
                RaisePropertyChanged("SelectedItem");
            }
        }

        MessageViewModel _selectedUnreadItem;
        public MessageViewModel SelectedUnreadItem
        {
            get
            {
                return _selectedUnreadItem;
            }
            set
            {
                // If the user deselects a "new" item
                if (_selectedUnreadItem != null)
                {
                    var tempItem = _selectedUnreadItem;
                    // Mark the item as read
                    tempItem.IsNew = false;
                    _unreadMessages.Remove(tempItem);
                }

                if (value != null && value.IsNew)
                {
                    value.IsNew = false;
                    _redditService.ReadMessage(value.Name);
                    var newMessageCount = _messages.OfType<MessageViewModel>().Count(message => message.IsNew);
                    HasMail = newMessageCount > 0;
                    _liveTileService.SetCount(newMessageCount);
                }

                _selectedUnreadItem = value;
                RaisePropertyChanged("SelectedUnreadItem");
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

        ObservableCollection<MessageViewModel> _unreadMessages = new ObservableCollection<MessageViewModel>();
        public ObservableCollection<MessageViewModel> UnreadMessages
        {
            get
            {
                return _unreadMessages;
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
