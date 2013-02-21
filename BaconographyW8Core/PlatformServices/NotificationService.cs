using BaconographyPortable.Messages;
using BaconographyPortable.Services;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace BaconographyW8.PlatformServices
{
    class NotificationService : INotificationService
    {
        public void CreateNotification(string text)
        {
            ToastTemplateType toastTemplate = ToastTemplateType.ToastText01;
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);

            XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");
            toastTextElements[0].AppendChild(toastXml.CreateTextNode(text));

            IXmlNode toastNode = toastXml.SelectSingleNode("/toast");
            ((XmlElement)toastNode).SetAttribute("launch", "{\"type\":\"toast\" }");

            ToastNotification toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

        public void CreateErrorNotification(Exception exception)
        {
            //we're no longer connected to the internet
            if (exception is System.Net.Http.HttpRequestException)
            {
                if(exception.Message.Contains("404"))
                {
                    var settingsService = ServiceLocator.Current.GetInstance<ISettingsService>();
                    if (settingsService.IsOnline())
                    {
                        ToastTemplateType toastTemplate = ToastTemplateType.ToastText01;
                        XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);

                        XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");
                        toastTextElements[0].AppendChild(toastXml.CreateTextNode("There doesnt seem to be anything here, 404 (Not Found)"));

                        IXmlNode toastNode = toastXml.SelectSingleNode("/toast");
                        ((XmlElement)toastNode).SetAttribute("launch", "{\"type\":\"toast\" }");

                        ToastNotification toast = new ToastNotification(toastXml);
                        ToastNotificationManager.CreateToastNotifier().Show(toast);
                    }
                }
                else
                {
                    var settingsService = ServiceLocator.Current.GetInstance<ISettingsService>();
                    if (settingsService.IsOnline())
                    {
                        ToastTemplateType toastTemplate = ToastTemplateType.ToastText01;
                        XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);

                        XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");
                        toastTextElements[0].AppendChild(toastXml.CreateTextNode("We're having a hard time connecting to reddit, you've been moved to offline mode"));

                        IXmlNode toastNode = toastXml.SelectSingleNode("/toast");
                        ((XmlElement)toastNode).SetAttribute("launch", "{\"type\":\"toast\" }");

                        ToastNotification toast = new ToastNotification(toastXml);
                        ToastNotificationManager.CreateToastNotifier().Show(toast);

                        Messenger.Default.Send<ConnectionStatusMessage>(new ConnectionStatusMessage { IsOnline = false, UserInitiated = false });
                    }
                }
            }
            else
            {
                ToastTemplateType toastTemplate = ToastTemplateType.ToastText01;
                XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);

                XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");
                toastTextElements[0].AppendChild(toastXml.CreateTextNode("We're having a hard time connecting to reddit, you might want to try again later or go into offline mode"));

                IXmlNode toastNode = toastXml.SelectSingleNode("/toast");
                ((XmlElement)toastNode).SetAttribute("launch", "{\"type\":\"toast\" }");

                ToastNotification toast = new ToastNotification(toastXml);
                ToastNotificationManager.CreateToastNotifier().Show(toast);
            }
        }


        public void CreateKitaroDBNotification(string text)
        {
            ToastTemplateType toastTemplate = ToastTemplateType.ToastImageAndText01;
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);

            XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");
            toastTextElements[0].AppendChild(toastXml.CreateTextNode(text));

            XmlElement imageNode = (XmlElement)toastXml.GetElementsByTagName("image")[0];
            imageNode.SetAttribute("id", "1");
            imageNode.SetAttribute("src", @"Assets/BaconographyKitaroPlug.png");

            IXmlNode toastNode = toastXml.SelectSingleNode("/toast");
            ((XmlElement)toastNode).SetAttribute("launch", "{\"type\":\"toast\" }");

            ToastNotification toast = new ToastNotification(toastXml);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }
    }
}
