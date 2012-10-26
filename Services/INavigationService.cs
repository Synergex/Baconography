using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Baconography.Services
{
    public interface INavigationService
    {
        void Init(Frame frame);
        void GoBack();
        void GoForward();
        bool Navigate<T>(object parameter);
        bool Navigate(Type source, object parameter);
    }

    public class NavigationService : INavigationService
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
    }
}
