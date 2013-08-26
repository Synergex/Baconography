using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using BaconographyWP8Core;
using GalaSoft.MvvmLight.Ioc;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;
using Microsoft.Practices.ServiceLocation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Navigation;
using Windows.System;

namespace BaconographyWP8.PlatformServices
{
    //threw on an 's' to avoid naming conflicts in very common namespaces
    public class NavigationServices : INavigationService
    {
        Frame _frame;
        public void Init(Frame frame)
        {
            _frame = frame;
        }

        public void GoBack()
        {
            try
            {
                _frame.GoBack();
            }
            catch
            {
                //whatever the failure was we need to ignore it, it was an invalid request and
                //msdn seems to suggest that this is not really a bug in user code
            }
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
			if (parameter is NavigateToUrlMessage)
			{
				var targetUri = new Uri((parameter as NavigateToUrlMessage).TargetUrl, UriKind.Absolute);
				WebBrowserTask webTask = new WebBrowserTask();
				webTask.Uri = targetUri;
				webTask.Show();
				return true;
			}

			if (parameter is SelectSubredditMessage)
			{
				var viewLocator = SimpleIoc.Default.GetService(typeof(IDynamicViewLocator)) as IDynamicViewLocator;
				source = viewLocator.MainView;
				var temp = parameter as SelectSubredditMessage;
				parameter = new SelectTemporaryRedditMessage
					{
						Subreddit = temp.Subreddit
					};

                //if we're already on the main page there is no reason to push context
                if (_frame != null && _frame.Content != null && source.IsAssignableFrom(_frame.Content.GetType()))
                {
                    Messenger.Default.Send<SelectTemporaryRedditMessage>(parameter as SelectTemporaryRedditMessage);
                    return true;
                }
			}

            var uriAttribute = source.GetCustomAttributes(typeof(ViewUriAttribute), true).FirstOrDefault() as ViewUriAttribute;
            if (uriAttribute != null)
            {
                var data = parameter != null ? JsonConvert.SerializeObject(parameter) : "";
				var uri = uriAttribute._targetUri + "?data=" + HttpUtility.UrlEncode(data);

				if (Uri.IsWellFormedUriString(uri, UriKind.Relative))
				{
					var targetUri = parameter != null ? new Uri(uri, UriKind.Relative) : new Uri(uriAttribute._targetUri, UriKind.Relative);
					return _frame.Navigate(targetUri);
				}
				else
				{
					throw new NotImplementedException("Handle a bad URI");
				}
            }
            else
            {
                throw new NotImplementedException("cant navigate to a view that doesnt have ViewUriAttribute");
            }
        }

        public void NavigateToSecondary(Type source, object parameter)
        {
            Navigate(source, parameter);
        }

        public async void NavigateToExternalUri(Uri uri)
        {
            ServiceLocator.Current.GetInstance<ISuspensionService>().FireSuspending();
            await Launcher.LaunchUriAsync(uri);
        }


        public void RemoveBackEntry()
        {
            if (((PhoneApplicationFrame)_frame).CanGoBack)
            {
                ((PhoneApplicationFrame)_frame).RemoveBackEntry();
            }
        }
    }
}
