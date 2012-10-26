using GalaSoft.MvvmLight;
using Microsoft.Practices.ServiceLocation;
using Baconography.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.ViewModel
{
    public class ContentPreferencesViewModel : ViewModelBase
    {
        private Nullable<bool> _allowNsfwContent;
        public bool AllowNSFWContent
        {
            get
            {
                if (_allowNsfwContent == null)
                {
                    var getUserTask = ServiceLocator.Current.GetInstance<IUsersService>().GetUser();
                    getUserTask.Wait();
                    _allowNsfwContent = getUserTask.Result.AllowOver18;
                }
                return _allowNsfwContent ?? false;
            }
            set
            {
                _allowNsfwContent = value;

                var getUserTask = ServiceLocator.Current.GetInstance<IUsersService>().GetUser();
                getUserTask.Wait();
                getUserTask.Result.AllowOver18 = value;
                RaisePropertyChanged("AllowNSFWContent");
            }
        }

        private Nullable<bool> _offlineOnlyGetsFirstSet;
        public bool OfflineOnlyGetsFirstSet
        {
            get
            {
                if (_offlineOnlyGetsFirstSet == null)
                {
                    var getUserTask = ServiceLocator.Current.GetInstance<IUsersService>().GetUser();
                    getUserTask.Wait();
                    _offlineOnlyGetsFirstSet = getUserTask.Result.OfflineOnlyGetsFirstSet;
                }
                return _offlineOnlyGetsFirstSet ?? false;
            }
            set
            {
                _offlineOnlyGetsFirstSet = value;

                var getUserTask = ServiceLocator.Current.GetInstance<IUsersService>().GetUser();
                getUserTask.Wait();
                getUserTask.Result.OfflineOnlyGetsFirstSet = value;
                RaisePropertyChanged("OfflineOnlyGetsFirstSet");
            }
        }

        private Nullable<int> _maxTopLevelOfflineComments;
        public int MaxTopLevelOfflineComments
        {
            get
            {
                if (_maxTopLevelOfflineComments == null)
                {
                    var getUserTask = ServiceLocator.Current.GetInstance<IUsersService>().GetUser();
                    getUserTask.Wait();
                    _maxTopLevelOfflineComments = getUserTask.Result.MaxTopLevelOfflineComments;
                }
                return _maxTopLevelOfflineComments ?? 250;
            }
            set
            {
                _maxTopLevelOfflineComments = value;

                var getUserTask = ServiceLocator.Current.GetInstance<IUsersService>().GetUser();
                getUserTask.Wait();
                getUserTask.Result.MaxTopLevelOfflineComments = value;
                RaisePropertyChanged("MaxTopLevelOfflineComments");
            }
        }

    }
}
