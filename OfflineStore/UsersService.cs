using GalaSoft.MvvmLight.Messaging;
using KitaroDB;
using Newtonsoft.Json;
using Baconography.Messages;
using Baconography.RedditAPI;
using Baconography.RedditAPI.Things;
using Baconography.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Baconography.OfflineStore
{
    class UsersService : IUsersService
    {
        User _currentUser;
        public async Task<User> GetUser()
        {
            if (!_initTask.IsCompleted)
                await _initTask;

            return _currentUser;
        }

        public async Task<User> TryLogin(string username, string password)
        {
            var result = await DoLogin(username, password);
            if (result != null)
            {
                Messenger.Default.Send<UserLoggedIn>(new UserLoggedIn { CurrentUser = result });
                _currentUser = result;
            }
            return result;
        }

        public async Task<User> TryStoredLogin(string username)
        {
            var result = await DoLogin(username);
            if (result != null)
            {
                Messenger.Default.Send<UserLoggedIn>(new UserLoggedIn { CurrentUser = result });
                _currentUser = result;
            }
            return result;
        }

        public void Logout()
        {
            _currentUser = CreateAnonUser();
            Messenger.Default.Send<UserLoggedIn>(new UserLoggedIn { CurrentUser = _currentUser });
        }

        Task _initTask;

        public Task Init()
        {
            if (_initTask == null)
            {
                _initTask = InitImpl();
            }
            return _initTask;
        }

        private async Task InitImpl()
        {
            _currentUser = await TryDefaultUser();
            if (_currentUser == null)
                _currentUser = CreateAnonUser();

            Messenger.Default.Send<UserLoggedIn>(new UserLoggedIn { CurrentUser = _currentUser });
        }

        public async Task<List<UserCredential>> StoredCredentials()
        {
            if (_storedCredentials == null)
                _storedCredentials = GetStoredCredentialsImpl();

            return await _storedCredentials;
        }

        public async Task AddStoredCredential(UserCredential newCredential, string password)
        {
            var userInfoDb = await GetUserInfoDB();

            var currentCredentials = await StoredCredentials();
            var existingCredential = currentCredentials.FirstOrDefault(credential => credential.Username == newCredential.Username);
            if (existingCredential != null)
            {
                //we already exist in the credentials, just update our login token and password (if its set)
                if (existingCredential.LoginCookie != newCredential.LoginCookie ||
                    existingCredential.IsDefault != newCredential.IsDefault)
                {
                    existingCredential.LoginCookie = newCredential.LoginCookie;
                    existingCredential.IsDefault = newCredential.IsDefault;

                    //go find the one we're updating and actually do it
                    var userCredentialsCursor = await userInfoDb.SelectAsync(userInfoDb.GetKeys().First(), "credentials", DBReadFlags.NoLock);
                    if (userCredentialsCursor != null)
                    {
                        using (userCredentialsCursor)
                        {
                            do
                            {
                                var credential = JsonConvert.DeserializeObject<UserCredential>(userCredentialsCursor.GetString());
                                if (credential.Username == newCredential.Username)
                                {
                                    await userCredentialsCursor.UpdateAsync(JsonConvert.SerializeObject(existingCredential));
                                    break;
                                }
                            } while (await userCredentialsCursor.MoveNextAsync());
                        }
                    }
                }
                if (!string.IsNullOrWhiteSpace(password))
                {
                    AddOrUpdateWindowsCredential(existingCredential, password);
                }
            }
            else
            {
                await userInfoDb.InsertAsync("credentials", JsonConvert.SerializeObject(newCredential));
                //force a re-get of the credentials next time someone wants them
                _storedCredentials = null;
            }
        }

        public async Task RemoveStoredCredential(string username)
        {
            var userInfoDb = await GetUserInfoDB();
            //go find the one we're updating and actually do it
            var userCredentialsCursor = await userInfoDb.SelectAsync(userInfoDb.GetKeys().First(), "credentials", DBReadFlags.NoLock);
            if (userCredentialsCursor != null)
            {
                using (userCredentialsCursor)
                {
                    do
                    {
                        var credential = JsonConvert.DeserializeObject<UserCredential>(userCredentialsCursor.GetString());
                        if (credential.Username == username)
                        {
                            await userCredentialsCursor.DeleteAsync();
                        }
                    } while (await userCredentialsCursor.MoveNextAsync());
                }
            }

            var passwordVault = new Windows.Security.Credentials.PasswordVault();
            try
            {
                var windowsCredentials = passwordVault.FindAllByResource("Baconography");
                var matchingWindowsCredential = windowsCredentials.FirstOrDefault(windowsCredential => string.Compare(windowsCredential.UserName, username, StringComparison.CurrentCultureIgnoreCase) == 0);
                if (matchingWindowsCredential != null)
                {
                    passwordVault.Remove(matchingWindowsCredential);
                }
            }
            catch
            {
            }
        }

        private string userInfoDbPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\userinfodb.ism";

        private DB _userInfoDb;
        private async Task<DB> GetUserInfoDB()
        {
            if (_userInfoDb == null)
            {
                _userInfoDb = await DB.CreateAsync(userInfoDbPath, DBCreateFlags.None, ushort.MaxValue - 100,
                    new DBKey[] { new DBKey(8, 0, DBKeyFlags.KeyValue, "default", true, false, false, 0) });
            }
            return _userInfoDb;
        }


        private User CreateAnonUser()
        {
            return new User(string.Empty, string.Empty, string.Empty, string.Empty);
        }

        private async Task<User> ConvertToRealUser(User anon, Func<Task<Tuple<string, string>>> aquireLogin)
        {
            //disregard anon, aquire login 
            var realUser = await CreateUserAsync(aquireLogin);
            return realUser;
        }

        private void AddOrUpdateWindowsCredential(UserCredential existingCredential, string password)
        {
            var passwordVault = new Windows.Security.Credentials.PasswordVault();
            try
            {
                var windowsCredentials = passwordVault.FindAllByResource("Baconography");
                var matchingWindowsCredential = windowsCredentials.FirstOrDefault(credential => string.Compare(credential.UserName, existingCredential.Username, StringComparison.CurrentCultureIgnoreCase) == 0);
                if (matchingWindowsCredential != null)
                {
                    matchingWindowsCredential.RetrievePassword();
                    if (matchingWindowsCredential.Password != password)
                    {
                        passwordVault.Remove(matchingWindowsCredential);
                    }
                    else
                        passwordVault.Add(new Windows.Security.Credentials.PasswordCredential("Baconography", existingCredential.Username, password));
                }
                else
                    passwordVault.Add(new Windows.Security.Credentials.PasswordCredential("Baconography", existingCredential.Username, password));
            }
            catch
            {
                passwordVault.Add(new Windows.Security.Credentials.PasswordCredential("Baconography", existingCredential.Username, password));
            }
        }

        private Task<List<UserCredential>> _storedCredentials;
        private async Task<List<UserCredential>> GetStoredCredentialsImpl()
        {
            List<UserCredential> credentials = new List<UserCredential>();
            var userInfoDb = await GetUserInfoDB();
            var userCredentialsCursor = await userInfoDb.SelectAsync(userInfoDb.GetKeys().First(), "credentials", DBReadFlags.NoLock);
            if (userCredentialsCursor != null)
            {
                using (userCredentialsCursor)
                {
                    do
                    {
                        var credential = JsonConvert.DeserializeObject<UserCredential>(userCredentialsCursor.GetString());
                        credentials.Add(credential);
                    } while (await userCredentialsCursor.MoveNextAsync());
                }
            }
            return credentials;
        }

        private async Task<User> LoginWithCredentials(UserCredential credential)
        {
            var meString = await User.GetMeAsync(credential.Username, credential.LoginCookie);
            //check if we're a valid login cookie by trying to get me.json
            if (!string.IsNullOrWhiteSpace(meString) && meString != "{}")
                return new User(credential.Username, "", credential.LoginCookie, meString);
            else
            {
                //we dont currently posses a valid login cookie, see if windows has a stored credential we can use for this username
                var passwordVault = new Windows.Security.Credentials.PasswordVault();
                try
                {
                    var windowsCredentials = passwordVault.FindAllByResource("Baconography");
                    var matchingWindowsCredential = windowsCredentials.FirstOrDefault(windowsCredential => string.Compare(windowsCredential.UserName, credential.Username, StringComparison.CurrentCultureIgnoreCase) == 0);
                    if (matchingWindowsCredential != null)
                    {
                        matchingWindowsCredential.RetrievePassword();
                        return await DoLogin(matchingWindowsCredential.UserName, matchingWindowsCredential.Password);
                    }
                }
                catch
                {
                }
            }
            return null;
        }

        //takes a delegate that prompts for username/password returning it in that order in the tuple
        private async Task<User> CreateUserAsync(Func<Task<Tuple<string, string>>> aquireLogin)
        {
            var defaultCredential = await TryDefaultUser();
            if (defaultCredential == null)
            {
                var loginCredentials = await aquireLogin();
                return await DoLogin(loginCredentials.Item1, loginCredentials.Item2);
            }
            else
                return defaultCredential;
        }

        private async Task<User> TryDefaultUser()
        {
            var credentials = await StoredCredentials();
            var defaultCredential = credentials.FirstOrDefault(credential => credential.IsDefault);
            if (defaultCredential != null)
            {
                var result = await LoginWithCredentials(defaultCredential);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        private async Task<User> DoLogin(string username)
        {
            var storedCredentials = await StoredCredentials();
            var targetCredential = storedCredentials.FirstOrDefault(credential => string.Compare(credential.Username, username, StringComparison.CurrentCultureIgnoreCase) == 0);

            if (targetCredential != null)
            {
                var theUser = await LoginWithCredentials(targetCredential);
                return theUser;
            }
            return null;
        }

        private async Task<User> DoLogin(string username, string password)
        {
            //we need to set up the httpclient to be cookie aware, this seems to be the simplist way
            var loginClientHandler = new HttpClientHandler { CookieContainer = new CookieContainer() };
            var loginClient = new HttpClient(loginClientHandler);
            loginClient.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Baconography", "1.0"));
            var loginUri = "http://www.reddit.com/api/login/" + username;
            var postContent = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "api_type", "json" },
                    { "user", username },
                    { "passwd", password }
                });
            var loginResult = await loginClient.PostAsync(loginUri, postContent);
            if (loginResult.IsSuccessStatusCode)
            {
                var jsonResult = await loginResult.Content.ReadAsStringAsync();
                var loginResultThing = JsonConvert.DeserializeObject<LoginJsonThing>(jsonResult);
                if (loginResultThing.Json == null ||
                    (loginResultThing.Json.Errors != null && loginResultThing.Json.Errors.Length != 0))
                    return null; //errors in the login process
                else
                {
                    //need to store everything in the db
                    var loginCookies = loginClientHandler.CookieContainer.GetCookies(new Uri(loginUri));
                    var loginCookie = loginCookies["reddit_session"].Value;
                    var meString = await User.GetMeAsync(username, loginCookie);

                    //for whatever reason they dont always respond here so at least get the modhash as its required for most things
                    if (string.IsNullOrWhiteSpace(meString) || meString == "{}")
                        meString = JsonConvert.SerializeObject(new Thing { Kind = "t2", Data = new Account { ModHash = loginResultThing.Json.Data.Modhash } });

                    var theUser = new User(username, password, loginCookie,
                        meString);

                    return theUser;
                }
            }
            else
                throw new Exception(loginResult.StatusCode.ToString());
        }
    }
}
