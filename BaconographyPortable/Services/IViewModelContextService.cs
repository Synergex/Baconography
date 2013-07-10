using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Services
{
    public interface IViewModelContextService
    {
        void PushViewModelContext(ViewModelBase viewModel);
        void PopViewModelContext();
        void PopViewModelContext(ViewModelBase viewModel);
        ViewModelBase Context { get; }
        IEnumerable<ViewModelBase> ContextStack { get; }
    }
}
