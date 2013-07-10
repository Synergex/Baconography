
using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
using BaconographyWP8Core;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
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
	[ViewUri("/View/CaptchaPageView.xaml")]
    public sealed partial class CaptchaPageView : PhoneApplicationPage
    {
		INavigationService _navigationService;

        public CaptchaPageView()
        {
            this.InitializeComponent();
			_navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
        }

		protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
		{
            UpdateMenuItems();
		}

        private void Send_Click(object sender, EventArgs e)
        {
            var vm = this.DataContext as CaptchaViewModel;
            if (vm != null)
                vm.Send.Execute(null);
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            // TODO: ARE YOU SURE?!?!?!
            _navigationService.GoBack();
        }

        private List<ApplicationBarIconButton> _appBarButtons;
        private void BuildAppBar()
        {
            _appBarButtons = new List<ApplicationBarIconButton>();

            _appBarButtons.Add(new ApplicationBarIconButton());
            _appBarButtons[0].IconUri = new Uri("\\Assets\\Icons\\send.png", UriKind.Relative);
            _appBarButtons[0].Text = "send";
            _appBarButtons[0].IsEnabled = false;
            _appBarButtons[0].Click += Send_Click;

            _appBarButtons.Add(new ApplicationBarIconButton());
            _appBarButtons[1].IconUri = new Uri("\\Assets\\Icons\\cancel.png", UriKind.Relative);
            _appBarButtons[1].Text = "cancel";
            _appBarButtons[1].IsEnabled = true;
            _appBarButtons[1].Click += Cancel_Click;

            ApplicationBar.Buttons.Clear();
            foreach (var button in _appBarButtons)
                ApplicationBar.Buttons.Add(button as IApplicationBarIconButton);
        }

        private void UpdateMenuItems()
        {
            if (_appBarButtons == null || ApplicationBar.Buttons.Count == 0)
                BuildAppBar();

            var vm = this.DataContext as CaptchaViewModel;
            if (vm != null)
                _appBarButtons[0].IsEnabled = vm.CanSend;
        }

        private void TextBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            BindingExpression bindingExpression = ((TextBox)sender).GetBindingExpression(TextBox.TextProperty);
            if (bindingExpression != null)
            {
                bindingExpression.UpdateSource();
            }

            UpdateMenuItems();
        }

    }
}
