using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Services
{
    public interface INavigationService
    {
        void GoBack();
        void GoForward();
        bool Navigate<T>(object parameter);
        bool Navigate(Type source, object parameter);
    }
}
