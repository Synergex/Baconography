﻿using BaconographyPortable.Messages;
using BaconographyPortable.Model.Reddit;
using BaconographyPortable.Services;
using GalaSoft.MvvmLight.Messaging;
using KitaroDB;
using Microsoft.Practices.ServiceLocation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System.Threading;

namespace BaconographyWP8.PlatformServices
{
    public class UserService : IUserService, BaconProvider.IBaconService
    {
        IRedditService _redditService;
        User _currentUser;

        public async Task<User> GetUser()
        {
            //if (!_initTask.IsCompleted)
            //    await _initTask;

			if(_currentUser == null)
				_currentUser = CreateAnonUser();

            return _currentUser;
        }

        public async Task<User> TryLogin(string username, string password)
        {
            var result = await _redditService.Login(username, password);
            if (result != null)
            {
				_currentUser = result;
                Messenger.Default.Send<UserLoggedInMessage>(new UserLoggedInMessage { CurrentUser = result, UserTriggered = true });
            }
            return result;
        }

        public async Task<User> TryStoredLogin(string username)
        {
            var result = await DoLogin(username, true);
            if (result != null)
            {
				_currentUser = result;
                Messenger.Default.Send<UserLoggedInMessage>(new UserLoggedInMessage { CurrentUser = result, UserTriggered = true });
            }
            return result;
        }

        public void Logout()
        {
            _currentUser = CreateAnonUser();
            Messenger.Default.Send<UserLoggedInMessage>(new UserLoggedInMessage { CurrentUser = _currentUser, UserTriggered = true });
        }

        private User CreateAnonUser()
        {
            return new User();
        }

        Task _initTask;

        public Task Initialize(IBaconProvider baconProvider)
        {
            if (_initTask == null)
            {
                _initTask = InitImpl(baconProvider.GetService<IRedditService>());
            }
            return _initTask;
        }

        private async Task InitImpl(IRedditService redditService)
        {
            _redditService = redditService;

            _currentUser = await TryDefaultUser();
            if (_currentUser == null)
                _currentUser = CreateAnonUser();

            Messenger.Default.Send<UserLoggedInMessage>(new UserLoggedInMessage { CurrentUser = _currentUser, UserTriggered = false });
        }

        public async Task<IEnumerable<UserCredential>> StoredCredentials()
        {
            if (_storedCredentials == null)
                _storedCredentials = GetStoredCredentialsImpl();

            return await _storedCredentials;
        }

        public async Task AddStoredCredential(UserCredential newCredential, string password)
        {
            var userInfoDb = await GetUserInfoDB();

			try
			{
				var currentCredentials = await StoredCredentials();
				var existingCredential = currentCredentials.FirstOrDefault(credential => credential.Username == newCredential.Username);
				if (existingCredential != null)
				{
					var lastCookie = existingCredential.LoginCookie;
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
						AddOrUpdateWindowsCredential(existingCredential, password, lastCookie);
					}
				}
				else
				{
					await userInfoDb.InsertAsync("credentials", JsonConvert.SerializeObject(newCredential));
					var newPassData = new PasswordData { Password = password, LastCookie = newCredential.LoginCookie };
					await userInfoDb.InsertAsync("passwords", JsonConvert.SerializeObject(newPassData));
					//force a re-get of the credentials next time someone wants them
					_storedCredentials = null;
				}
			}
			catch
			{
				//let it fail
			}
        }

        public async Task RemoveStoredCredential(string username)
        {
			try
			{
				var userInfoDb = await GetUserInfoDB();
				List<string> lastCookies = new List<string>();
				//go find the one we're updating and actually do it
				var userCredentialsCursor = await userInfoDb.SelectAsync(userInfoDb.GetKeys().First(), "credentials", DBReadFlags.AutoLock);
				if (userCredentialsCursor != null)
				{
					using (userCredentialsCursor)
					{
						do
						{
							var credential = JsonConvert.DeserializeObject<UserCredential>(userCredentialsCursor.GetString());
							if (credential.Username == username)
							{
								lastCookies.Add(credential.LoginCookie);
								await userCredentialsCursor.DeleteAsync();
							}
						} while (await userCredentialsCursor.MoveNextAsync());
					}
				}

                var passwordCursor = await userInfoDb.SelectAsync(userInfoDb.GetKeys().First(), "passwords", DBReadFlags.AutoLock);
				if (passwordCursor != null)
				{
					using (passwordCursor)
					{
						do
						{
							var passwordData = JsonConvert.DeserializeObject<PasswordData>(passwordCursor.GetString());
							if (lastCookies.Contains(passwordData.LastCookie))
							{
								await passwordCursor.DeleteAsync();
							}
						} while (await passwordCursor.MoveNextAsync());
					}
				}
			}
			catch
			{
				//let it fail
			}

        }

        private async void AddOrUpdateWindowsCredential(UserCredential existingCredential, string password, string lastCookie)
        {
			var userInfoDb = await GetUserInfoDB();
			try
			{

				var passwordCursor = await userInfoDb.SelectAsync(userInfoDb.GetKeys().First(), "passwords", DBReadFlags.AutoLock);
				if (passwordCursor != null)
				{
					using (passwordCursor)
					{
						do
						{
							var passwordData = JsonConvert.DeserializeObject<PasswordData>(passwordCursor.GetString());
							if (lastCookie == passwordData.LastCookie)
							{
								var newPassData = new PasswordData { Password = password, LastCookie = existingCredential.LoginCookie };
								await passwordCursor.UpdateAsync(JsonConvert.SerializeObject(newPassData));
								break;
							}
						} while (await passwordCursor.MoveNextAsync());
					}
				}
			}
			catch
			{
				//let it fail
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

        private async Task<User> LoginWithCredentials(UserCredential credential, bool userInitiated)
        {
            var originalCookie = credential.LoginCookie;
            if (!string.IsNullOrWhiteSpace(credential.LoginCookie))
            {
                var loggedInUser = new User { Username = credential.Username, LoginCookie = credential.LoginCookie, NeedsCaptcha = false };
                if (userInitiated)
                {
                    loggedInUser.Me = await _redditService.GetMe(loggedInUser);
                }
                else
                {
                    ThreadPool.RunAsync(async (o) =>
                    {
                        await Task.Delay(5000);
                        try
                        {
                            loggedInUser.Me = await _redditService.GetMe(loggedInUser);
                        }
                        catch { }
                        if (loggedInUser.Me == null)
                            ServiceLocator.Current.GetInstance<INotificationService>().CreateNotification(string.Format("Failed to login with user {0}", credential.Username));
                    });
                }
                return loggedInUser;
            }
            else
            {
                //we dont currently posses a valid login cookie, see if windows has a stored credential we can use for this username
				var userInfoDb = await GetUserInfoDB();
				var passwordCursor = await userInfoDb.SelectAsync(userInfoDb.GetKeys().First(), "passwords", DBReadFlags.NoLock);
				if (passwordCursor != null)
				{
					using (passwordCursor)
					{
						do
						{
							try
							{
								var passwordData = JsonConvert.DeserializeObject<PasswordData>(passwordCursor.GetString());
								if (credential.LoginCookie == passwordData.LastCookie)
								{
									return await _redditService.Login(credential.Username, passwordData.Password);
								}
							}
							catch
							{

							}
						} while (await passwordCursor.MoveNextAsync());
					}
				}
            }
            return null;
        }

        private async Task<User> TryDefaultUser()
        {
            var credentials = await StoredCredentials();
            var defaultCredential = credentials.FirstOrDefault(credential => credential.IsDefault);
            if (defaultCredential != null)
            {
                var result = await LoginWithCredentials(defaultCredential, false);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        private async Task<User> DoLogin(string username, bool userInitiated)
        {
            var storedCredentials = await StoredCredentials();
            var targetCredential = storedCredentials.FirstOrDefault(credential => string.Compare(credential.Username, username, StringComparison.CurrentCultureIgnoreCase) == 0);

            if (targetCredential != null)
            {
                var theUser = await LoginWithCredentials(targetCredential, userInitiated);
                return theUser;
            }
            return null;
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

        public void CloseDB()
        {
            _userInfoDb.Dispose();
            _userInfoDb = null;
        }

		private class PasswordData
		{
			[JsonProperty("lastcookie")]
			public string LastCookie { get; set; }
			[JsonProperty("password")]
			public string Password { get; set; }
		}
    }
}
