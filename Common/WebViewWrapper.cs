using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Baconography.Common
{
    //not currently used, seems to be the only way to get a sneak peak into the workings of the WebView
    public class WebViewWrapper : IDisposable
    {
        // Maintain a reference to the WebView control so that 
        // we can invoke javascript 
        public WebView WebView { get; private set; }

        public WebViewWrapper(WebView webView)
        {
            WebView = webView;
        }

        public string CurrentUrl
        {
            get
            {
                try
                {
                    var retrieveHtml = "location.href;";
                    var html = WebView.InvokeScript("eval", new[] { retrieveHtml });
                    return html;
                }
                catch
                {
                    if (WebView.Source != null)
                        return WebView.Source.ToString();
                    else
                        return null;
                }
            }
            set
            {
                try
                {
                    var retrieveHtml = string.Format("location.href = {0};", value);
                    var html = WebView.InvokeScript("eval", new[] { retrieveHtml });
                }
                catch
                {
                    WebView.Source = new Uri(value);
                }
            }
        }

        public void Dispose()
        {
            WebView = null;
        }
    }

    public class NavigatingEventArgs : EventArgs
    {
        public Uri LeavingUri { get; set; }
    }
}
