using BaconographyPortable.Services;
using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyWP8.PlatformServices
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
                    var html = ((WebBrowser)WebView).InvokeScript("eval", new[] { retrieveHtml });
                    return html as string;
                }
                catch
                {
                    if (((WebBrowser)WebView).Source != null)
                        return ((WebBrowser)WebView).Source.ToString();
                    else
                        return null;
                }
            }
            set
            {
                try
                {
                    var retrieveHtml = string.Format("location.href = {0};", value);
                    var html = ((WebBrowser)WebView).InvokeScript("eval", new[] { retrieveHtml });
                }
                catch
                {
                    ((WebBrowser)WebView).Source = new Uri(value);
                }
            }
        }

        public void Disable()
        {
            _webView.NavigateToString("<!DOCTYPE html><html xmlns='http://www.w3.org/1999/xhtml'></html>");
        }

        WebBrowser _webView;
        public object WebView
        {
            get
            {
                if (_webView == null)
                {
                    //TODO: hook up all the events
                    _webView = new WebBrowser();
                }
                return _webView;
            }
        }
    }
}
