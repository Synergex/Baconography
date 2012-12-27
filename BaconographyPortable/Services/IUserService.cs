using BaconographyPortable.Model.Reddit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyPortable.Services
{
    public interface IUserService
    {
        Task<User> GetUser();
        Task<User> TryLogin(string username, string password);
        Task<User> TryStoredLogin(string username);
        void Logout();
        Task<IEnumerable<UserCredential>> StoredCredentials();
        Task AddStoredCredential(UserCredential newCredential, string password);
        Task RemoveStoredCredential(string username);
    }
}
