using BaconographyPortable.Messages;
using BaconographyPortable.Services;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyWP8.ViewModel
{
    public class LockScreenViewModel
    {
        public LockScreenViewModel()
        {
            Messenger.Default.Register<SettingsChangedMessage>(this, settingsChanged);
        }

        private void settingsChanged(SettingsChangedMessage obj)
        {
            var settingsService = ServiceLocator.Current.GetInstance<ISettingsService>();
            OverlayOpacity = settingsService.OverlayOpacity;
        }

        public string ImageSource { get; set; }
        public IEnumerable<LockScreenMessage> OverlayItems  { get; set; }
        public float OverlayOpacity { get; set; }
    }

    public class LockScreenMessage
    {
        public string DisplayText { get; set; }
    }
}
