using BaconographyPortable.Messages;
using BaconographyPortable.Services;
using BaconographyW8.Common;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Xaml.Controls;

namespace BaconographyW8.PlatformServices
{
    class NavigationService : INavigationService
    {
        Frame _frame;
        public void Init(Frame frame)
        {
            _frame = frame;
        }

        public void GoBack()
        {
            _frame.GoBack();
        }

        public void GoForward()
        {
            _frame.GoForward();
        }

        public bool Navigate<T>(object parameter = null)
        {
            var type = typeof(T);

            return Navigate(type, parameter);
        }

        public bool Navigate(Type source, object parameter = null)
        {
            return _frame.Navigate(source, parameter);
        }

        public void NavigateToSecondary(Type source, object parameter)
        {
            var flyout = new SettingsFlyout();
            flyout.Content = Activator.CreateInstance(source);
            flyout.HeaderText = parameter as string;
            flyout.IsOpen = true;
            flyout.Closed += (e, sender) => Messenger.Default.Unregister<CloseSettingsMessage>(this);
            Messenger.Default.Register<CloseSettingsMessage>(this, (message) =>
            {
                flyout.IsOpen = false;
            });
        }

        public async void NavigateToExternalUri(Uri uri)
        {
            await Launcher.LaunchUriAsync(uri);
        }


        public void RemoveBackEntry()
        {
            throw new NotImplementedException();
        }
    }
}
