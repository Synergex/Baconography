using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyWP8Core
{
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    sealed class ViewUriAttribute : Attribute
    {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236
        public readonly string _targetUri;
    
        // This is a positional argument
        public ViewUriAttribute (string targetUri) 
        {
            _targetUri = targetUri; 
        }
    }
}
