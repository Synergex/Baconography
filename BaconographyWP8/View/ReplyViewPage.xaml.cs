
using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
using BaconographyWP8Core;
using Microsoft.Phone.Controls;
using Microsoft.Practices.ServiceLocation;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Windows.Foundation;
using Windows.Foundation.Collections;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BaconographyWP8.View
{
	[ViewUri("/View/ReplyViewPage.xaml")]
    public sealed partial class ReplyViewPage : PhoneApplicationPage
    {
		INavigationService _navigationService;

		public ReplyViewPage()
        {
            this.InitializeComponent();
			_navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
			_navigationService.GoBack();
        }

		protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
		{
			if (e.NavigationMode == System.Windows.Navigation.NavigationMode.Back)
			{
				var vm = this.DataContext as ReplyViewModel;
				if (vm != null)
					vm.RefreshUser.Execute(null);
			}
			base.OnNavigatedTo(e);
		}

		private void ShowMoreButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			ShowExtended = !ShowExtended;
		}

		private void SendButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			var vm = this.DataContext as ReplyViewModel;
			if (vm != null)
			{
				vm.Submit.Execute(null);
				_navigationService.Navigate(typeof(LoginPageView), null);
			}
		}

		public static readonly DependencyProperty ShowExtendedProperty =
			DependencyProperty.Register(
				"ShowExtended",
				typeof(bool),
				typeof(ReplyViewPage),
				new PropertyMetadata(false, OnShowExtendedPropertyChanged)
			);

		public bool ShowExtended
		{
			get
			{
				return (bool)GetValue(ShowExtendedProperty);
			}
			set
			{
				SetValue(ShowExtendedProperty, value);
			}
		}

		private static void OnShowExtendedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var view = (ReplyViewPage)d;
			view.ShowExtended = (bool)e.NewValue;
		}

		private void LoginButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			_navigationService.Navigate(typeof(LoginPageView), null);
		}

		private void TextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
		{

		}

		private void TextBox_SelectionChanged(object sender, RoutedEventArgs e)
		{
			var textbox = sender as TextBox;
			if (textbox != null)
			{
				var vm = this.DataContext as ReplyViewModel;
				if (vm != null)
				{
					if (vm.SelectionLength != textbox.SelectionLength)
						vm.SelectionLength = textbox.SelectionLength;
					if (vm.SelectionStart != textbox.SelectionStart)
						vm.SelectionStart = textbox.SelectionStart;
				}
			}
		}
    }
}
