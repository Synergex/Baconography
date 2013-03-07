using BaconographyPortable.Messages;
using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaconographyWP8.ViewModel
{
	public class LoginPageViewModel : LoginViewModel
	{
		INavigationService _navigationService;

		public LoginPageViewModel(IBaconProvider baconProvider)
			: base(baconProvider)
		{
			_navigationService = baconProvider.GetService<INavigationService>();
			MessengerInstance.Register<CloseSettingsMessage>(this, OnClosedSettings);
		}

		protected void OnClosedSettings(CloseSettingsMessage userMessage)
		{
			_navigationService.GoBack();
		}

		public async void LoadCredentials()
		{
			var storedCredentials = await _userService.StoredCredentials();
			var users = storedCredentials.Select(uc => uc.Username);
			Credentials.Clear();
			foreach (var credential in users)
				Credentials.Add(credential);
			RaisePropertyChanged("Credentials");
		}
	}
}
