using BaconographyPortable.Messages;
using BaconographyPortable.Services;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyWP8.PlatformServices
{
    class NotificationService : INotificationService
    {
        public void CreateNotification(string text)
        {
            ShellToast toast = new ShellToast();
            toast.Title = "Baconography";
            toast.Content = text;
            toast.Show();
        }

        public void CreateErrorNotification(Exception exception)
        {
            if (exception is System.Net.WebException)
            {
                ShellToast toast = new ShellToast();
                toast.Title = "Baconography";
                toast.Content = "We're having a hard time connecting to reddit, you've been moved to offline mode";
                toast.Show();
                Messenger.Default.Send<ConnectionStatusMessage>(new ConnectionStatusMessage { IsOnline = false, UserInitiated = false });
            }
            else
            {
                ShellToast toast = new ShellToast();
                toast.Title = "Baconography";
                toast.Content = "We're having a hard time connecting to reddit, you might want to try again later or go into offline mode";
                toast.Show();
            }
        }


        public void CreateKitaroDBNotification(string text)
        {
            ShellToast toast = new ShellToast();
            toast.Title = "Baconography-KitaroDB";
            toast.Content = text;
            toast.Show();
        }
    }
}
