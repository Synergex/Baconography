using BaconographyPortable.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel.Collections
{
    public class CommentViewModelCollection : BaseIncrementalLoadCollection<CommentViewModel>
    {
        protected override Task<IEnumerable<CommentViewModel>> InitialLoad(Dictionary<object, object> state)
        {
            throw new NotImplementedException();
        }

        protected override Task<IEnumerable<CommentViewModel>> LoadAdditional(Dictionary<object, object> state)
        {
            throw new NotImplementedException();
        }

        protected override bool HasAdditional(Dictionary<object, object> state)
        {
            return state.ContainsKey("After");
        }
    }
}
