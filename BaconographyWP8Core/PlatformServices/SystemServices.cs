using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using Windows.Networking.Connectivity;
using Windows.System.Threading;

namespace BaconographyWP8.PlatformServices
{
    class SystemServices : ISystemServices
    {
        public SystemServices()
        {
            NetworkInformation.NetworkStatusChanged += networkStatusChanged;
            networkStatusChanged(null);
        }

        private void networkStatusChanged(object sender)
        {
            var connectionProfile = NetworkInformation.GetInternetConnectionProfile();
            var connectionCostType = connectionProfile.GetConnectionCost().NetworkCostType;
            if (connectionCostType == NetworkCostType.Unknown || connectionCostType == NetworkCostType.Unrestricted)
                IsOnMeteredConnection = false;
            else
                IsOnMeteredConnection = true;

            IsNearingDataLimit = connectionProfile.GetConnectionCost().ApproachingDataLimit || connectionProfile.GetConnectionCost().OverDataLimit || connectionProfile.GetConnectionCost().Roaming;
        }

        public void StopTimer(object tickHandle)
        {
            if (tickHandle is DispatcherTimer)
            {
                if (((DispatcherTimer)tickHandle).IsEnabled)
                    ((DispatcherTimer)tickHandle).Stop();
            }
            else if (tickHandle is ThreadPoolTimer)
            {
                ((ThreadPoolTimer)tickHandle).Cancel();
            }
        }

        public async void RunAsync(Func<object, Task> action)
        {
            await AsyncInfo.Run((c) => action(c));
        }

        public object StartTimer(EventHandler<object> tickHandler, TimeSpan tickSpan, bool uiThread)
        {
            if (uiThread)
            {
                DispatcherTimer dt = new DispatcherTimer();
                dt.Tick += (sender, args) => tickHandler(sender, args);
                dt.Interval = tickSpan;
                dt.Start();
                return dt;
            }
            else
            {
                return ThreadPoolTimer.CreatePeriodicTimer((timer) => tickHandler(this, timer), tickSpan);
            }
        }

        public void RestartTimer(object tickHandle)
        {
            if (tickHandle is DispatcherTimer)
            {
                ((DispatcherTimer)tickHandle).Start();
            }
            else if (tickHandle is ThreadPoolTimer)
            {
                throw new NotImplementedException();
            }
        }


        public void StartThreadPoolTimer(Func<object, Task> action, TimeSpan timer)
        {
            ThreadPoolTimer.CreateTimer(async (obj) => await action(obj), timer);
        }

        public bool IsOnMeteredConnection { get; set; }
        public bool IsNearingDataLimit { get; set; }
    }
}
