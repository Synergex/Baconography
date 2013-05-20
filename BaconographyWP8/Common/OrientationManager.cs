using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GalaSoft.MvvmLight;
using BaconographyPortable.Services;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.ServiceLocation;
using BaconographyPortable.Messages;
using Microsoft.Phone.Controls;
using BaconographyWP8.Messages;

namespace BaconographyWP8.Common
{
	public class OrientationManager : ObservableObject
	{
		IBaconProvider _baconProvider;
		ISettingsService _settingsService;

		public OrientationManager()
		{
			_baconProvider = ServiceLocator.Current.GetInstance<IBaconProvider>();
			if (_baconProvider != null)
			{
				_settingsService = _baconProvider.GetService<ISettingsService>();
			}

			Messenger.Default.Register<SettingsChangedMessage>(this, OnSettingsChanged);
			Messenger.Default.Register<OrientationChangedMessage>(this, OnOrientationChanged);
		}

		private PageOrientation StringToOrientation(string orientation)
		{
			switch (orientation)
			{
				case "Landscape":
					return PageOrientation.Landscape;
				case "LandscapeLeft":
					return PageOrientation.LandscapeLeft;
				case "LandscapeRight":
					return PageOrientation.LandscapeRight;
				case "Portrait":
					return PageOrientation.Portrait;
				case "PortraitUp":
					return PageOrientation.PortraitUp;
				case "PortraitDown":
					return PageOrientation.PortraitDown;
				case "None":
				default:
					return PageOrientation.None;
			}
		}

		private void OnOrientationChanged(OrientationChangedMessage message)
		{
			switch (message.Orientation)
			{
				case PageOrientation.Landscape:
				case PageOrientation.LandscapeLeft:
				case PageOrientation.LandscapeRight:
					SystemTrayVisible = false;
					break;
				case PageOrientation.None:
				case PageOrientation.Portrait:
				case PageOrientation.PortraitDown:
				case PageOrientation.PortraitUp:
				default:
					SystemTrayVisible = true;
					break;
			}

			Orientation = message.Orientation;
		}

		private void OnSettingsChanged(SettingsChangedMessage message)
		{
			_orientationLocked = _settingsService.OrientationLock;
			var _orientation = StringToOrientation(_settingsService.Orientation);

			if (_orientationLocked)
			{
				switch (_orientation)
				{
					case PageOrientation.Landscape:
					case PageOrientation.LandscapeLeft:
					case PageOrientation.LandscapeRight:
						SupportedOrientation = SupportedPageOrientation.Landscape;
						SystemTrayVisible = false;
						break;
					case PageOrientation.None:
					case PageOrientation.Portrait:
					case PageOrientation.PortraitDown:
					case PageOrientation.PortraitUp:
					default:
						SupportedOrientation = SupportedPageOrientation.Portrait;
						break;
				}
				RaisePropertyChanged("Orientation");
			}
			else
			{
				SupportedOrientation = SupportedPageOrientation.PortraitOrLandscape;
			}
		}

		private bool _orientationLocked;
		public bool OrientationLocked
		{
			get { return _orientationLocked; }
			set
			{
				_orientationLocked = value;
				RaisePropertyChanged("OrientationLocked");
			}
		}

		private PageOrientation _orientation = PageOrientation.Portrait;
		public PageOrientation Orientation
		{
			get { return _orientation; }
			set
			{
				_orientation = value;
				RaisePropertyChanged("Orientation");
			}
		}

		private SupportedPageOrientation _supportedOrientation = SupportedPageOrientation.PortraitOrLandscape;
		public SupportedPageOrientation SupportedOrientation
		{
			get { return _supportedOrientation; }
			set
			{
				_supportedOrientation = value;
				RaisePropertyChanged("SupportedOrientation");
			}
		}

		private bool _systemTrayVisible;
		public bool SystemTrayVisible
		{
			get { return _systemTrayVisible; }
			set
			{
				_systemTrayVisible = value;
				RaisePropertyChanged("SystemTrayVisible");
			}
		}
	}
}
