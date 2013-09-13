using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Services
{
    public interface INotificationService
    {
        void CreateNotification(string text);
        void CreateNotificationWithNavigation(string text, Type navTarget, object navParameter);
        void CreateKitaroDBNotification(string text);
        void CreateErrorNotification(Exception exception);
    }
}
