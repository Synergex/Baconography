using BaconographyPortable.Common;
using BaconographyPortable.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.ViewModel.Collections
{
    public class StoredUserCredentialsCollection : BaseIncrementalLoadCollection<string>
    {
        IUserService _userService;
        public StoredUserCredentialsCollection(IBaconProvider baconProvider)
        {
            _userService = baconProvider.GetService<IUserService>();
        }

        protected override async Task<IEnumerable<string>> InitialLoad(Dictionary<object, object> state)
        {
            var storedCredentials = await _userService.StoredCredentials();

            return storedCredentials.Select(uc => uc.Username);
        }

        protected override Task<IEnumerable<string>> LoadAdditional(Dictionary<object, object> state)
        {
            throw new NotImplementedException();
        }

        protected override bool HasAdditional(Dictionary<object, object> state)
        {
            return false;
        }
    }
}
