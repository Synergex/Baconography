using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Services
{
    //this is really IServiceProvider but that name was already taken and it was undesirable to use the system one
    public interface IBaconProvider
    {
        T GetService<T>() where T : class; 
    }
}
