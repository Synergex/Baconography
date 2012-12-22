using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace BaconographyW8.PlatformServices
{
    class WebViewWrapper : IWebViewWrapper
    {
        public event Action<string> UrlChanged;

        public string Url
        {
            get
            {
                try
                {
                    var retrieveHtml = "location.href;";
                    var html = ((WebView)WebView).InvokeScript("eval", new[] { retrieveHtml });
                    return html;
                }
                catch
                {
                    if (((WebView)WebView).Source != null)
                        return ((WebView)WebView).Source.ToString();
                    else
                        return null;
                }
            }
            set
            {
                try
                {
                    var retrieveHtml = string.Format("location.href = {0};", value);
                    var html = ((WebView)WebView).InvokeScript("eval", new[] { retrieveHtml });
                }
                catch
                {
                    ((WebView)WebView).Source = new Uri(value);
                }
            }
        }

        public void Disable()
        {
            _webView.NavigateToString("<!DOCTYPE html><html xmlns='http://www.w3.org/1999/xhtml'></html>");
        }

        WebView _webView;
        public object WebView
        {
            get
            {
                if (_webView == null)
                {
                    //TODO: hook up all the events
                    _webView = new Windows.UI.Xaml.Controls.WebView();
                }
                return _webView;
            }
        }
    }
}
