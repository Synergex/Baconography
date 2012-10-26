using Baconography.RedditAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.Services
{
    public interface IUsersService
    {
        Task<User> GetUser();
        Task<User> TryLogin(string username, string password);
        Task<User> TryStoredLogin(string username);
        void Logout();
        Task<List<UserCredential>> StoredCredentials();
        Task Init();
        Task AddStoredCredential(UserCredential newCredential, string password);
        Task RemoveStoredCredential(string username);
    }

    public class UserCredential
    {
        public string LoginCookie { get; set; }
        public string Username { get; set; }
        public Thing Me { get; set; }
        public bool IsDefault { get; set; }
    }
}
