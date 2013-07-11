using BaconographyPortable.Model.Reddit;
using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Common
{
    public interface IMergableThing
    {
        bool MaybeMerge(ViewModelBase thing);
    }
}
