using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Services
{
    public interface IWebViewWrapper
    {
        event Action<string> UrlChanged;
        string Url { get; set; }
        void Disable();
    }
}
