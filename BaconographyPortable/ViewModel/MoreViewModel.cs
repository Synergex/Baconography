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
    public class MoreViewModel : ViewModelBase
    {
        IEnumerable<string> _ids;
        string _targetName;
        string _subreddit;
        CommentViewModel _parent;
        Action<IEnumerable<string>, ObservableCollection<ViewModelBase>, ViewModelBase> _loadMore;
        public MoreViewModel(IBaconProvider baconProvider, IEnumerable<string> ids, string targetName, string subreddit, Action<IEnumerable<string>, ObservableCollection<ViewModelBase>, ViewModelBase> loadMore, CommentViewModel parent)
        {
            _loadMore = loadMore;
            _parent = parent;
            _ids = ids;
            _targetName = targetName;
            _subreddit = subreddit;
            Count = _ids.Count();
            //TODO use the targetname to determine the kind for now its always going to be comments but
            //that might change in the future
            Kind = "comment";

            _triggerLoad = new RelayCommand(TriggerLoadImpl);
        }

        private void TriggerLoadImpl()
        {
            Loading = true;
            _loadMore(_ids, _parent.Replies, _parent);
            _parent.Replies.Remove(this);
        }

        public int Count { get; private set; }
        public string Kind { get; private set; }
        bool _loading;
        public bool Loading
        {
            get
            {
                return _loading;
            }
            set
            {
                _loading = value;
                RaisePropertyChanged("Loading");
            }
        }

        public string CountString
        {
            get
            {
                if (Count > 1)
                    return string.Format("({0} replies)", Count);
                else
                    return "(1 reply)";
            }
        }

        RelayCommand _triggerLoad;
        public RelayCommand TriggerLoad
        {
            get
            {
                return _triggerLoad;
            }
        }
    }
}
