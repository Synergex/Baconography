﻿using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Threading;
using Windows.UI.Xaml;

namespace BaconographyW8.PlatformServices
{
    class SystemServices : ISystemServices
    {
        public void StopTimer(object tickHandle)
        {
            if (tickHandle is DispatcherTimer)
            {
                if(((DispatcherTimer)tickHandle).IsEnabled)
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
                dt.Tick += tickHandler;
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
            throw new NotImplementedException();
        }

        public bool IsOnMeteredConnection
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsNearingDataLimit
        {
            get { throw new NotImplementedException(); }
        }
    }
}
