using BaconographyPortable.Messages;
using BaconographyPortable.Services;
using Coding4Fun.Toolkit.Controls;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace BaconographyWP8.PlatformServices
{
    class NotificationService : INotificationService
    {
        TaskScheduler _scheduler;
        public NotificationService()
        {
            _scheduler = TaskScheduler.FromCurrentSynchronizationContext();
        }

        public void CreateNotification(string text)
        {
            Task.Factory.StartNew(() =>
                {
                    ToastPrompt toast = new ToastPrompt();
                    toast.Title = "Baconography";
                    toast.Message = text;
                    toast.TextWrapping = System.Windows.TextWrapping.Wrap;
                    toast.ImageSource = new BitmapImage(new Uri("Assets\\ApplicationIconSmall.png", UriKind.RelativeOrAbsolute));
                    toast.Show();
                    
                }, System.Threading.CancellationToken.None, TaskCreationOptions.None, _scheduler); 
        }

        public void CreateErrorNotification(Exception exception)
        {
            Task.Factory.StartNew(() =>
                {
                    if (exception is System.Net.WebException)
                    {
                        ToastPrompt toast = new ToastPrompt();
                        toast.Title = "Baconography";
                        toast.Message = "We're having a hard time connecting to reddit";
                        toast.ImageSource = new BitmapImage(new Uri("Assets\\ApplicationIconSmall.png", UriKind.RelativeOrAbsolute));
                        toast.TextWrapping = System.Windows.TextWrapping.Wrap;
                        toast.Show();
                        Messenger.Default.Send<ConnectionStatusMessage>(new ConnectionStatusMessage { IsOnline = false, UserInitiated = false });
                    }
                    else if (exception.Message == "NotFound")
                    {
                        CreateNotification("There doesnt seem to be anything here");
                    }
                    else
                    {
                        ToastPrompt toast = new ToastPrompt();
                        toast.Title = "Baconography";
                        toast.Message = "We're having a hard time connecting to reddit, you might want to try again later";
                        toast.ImageSource = new BitmapImage(new Uri("Assets\\ApplicationIconSmall.png", UriKind.RelativeOrAbsolute));
                        toast.TextWrapping = System.Windows.TextWrapping.Wrap;
                        toast.Show();
                    }
                }, System.Threading.CancellationToken.None, TaskCreationOptions.None, _scheduler); 
        }


        public void CreateKitaroDBNotification(string text)
        {
            Task.Factory.StartNew(() =>
                {
                    ToastPrompt toast = new ToastPrompt();
                    toast.Title = "Baconography";
                    toast.Message = text;
                    toast.ImageSource = new BitmapImage(new Uri("Assets\\BaconographyKitaroPlug.png", UriKind.RelativeOrAbsolute));
                    toast.TextWrapping = System.Windows.TextWrapping.Wrap;
                    toast.Show();
                }, System.Threading.CancellationToken.None, TaskCreationOptions.None, _scheduler); 
        }
    }
}
