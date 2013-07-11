using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using BaconographyWP8Core;
using BaconographyWP8.ViewModel;
using Microsoft.Practices.ServiceLocation;
using BaconographyPortable.Services;
using BaconographyWP8.Common;
using GalaSoft.MvvmLight.Messaging;
using BaconographyPortable.Messages;
using BaconographyPortable.ViewModel;
using System.Windows.Media;

namespace BaconographyWP8.View
{
    [ViewUri("/View/MessagingPageView.xaml")]
	public partial class MessagingPageView : PhoneApplicationPage
	{

        public MessagingPageView()
		{
			InitializeComponent();
		}

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var vm = this.DataContext as MessagesViewModel;
            if (vm != null)
            {
                if (vm.HasMail)
                    pivot.SelectedIndex = 1;
            }
            UpdateMenuItems();
            base.OnNavigatedTo(e);
        }

		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			base.OnNavigatedFrom(e);
		}

        private void New_Click(object sender, EventArgs e)
        {
            var vm = this.DataContext as MessagesViewModel;
            if (vm != null)
            {
                vm.NewMessage.Execute(vm);
            }
        }

        private void Reply_Click(object sender, EventArgs e)
        {
            var vm = this.DataContext as MessagesViewModel;
            if (vm != null)
            {
                vm.ReplyToMessage.Execute(vm);
            }
        }

        private void Refresh_Click(object sender, EventArgs e)
        {
            var vm = this.DataContext as MessagesViewModel;
            if (vm != null)
            {
                vm.RefreshMessages.Execute(vm);
            }
        }

        private List<ApplicationBarIconButton> _appBarButtons;
        private void BuildAppBar()
        {
            _appBarButtons = new List<ApplicationBarIconButton>();

            _appBarButtons.Add(new ApplicationBarIconButton());
            _appBarButtons[0].IconUri = new Uri("\\Assets\\Icons\\new.png", UriKind.Relative);
            _appBarButtons[0].Text = "new";
            _appBarButtons[0].IsEnabled = true;
            _appBarButtons[0].Click += New_Click;

            _appBarButtons.Add(new ApplicationBarIconButton());
            _appBarButtons[1].IconUri = new Uri("\\Assets\\Icons\\reply.png", UriKind.Relative);
            _appBarButtons[1].Text = "reply";
            _appBarButtons[1].IsEnabled = false;
            _appBarButtons[1].Click += Reply_Click;

            _appBarButtons.Add(new ApplicationBarIconButton());
            _appBarButtons[2].IconUri = new Uri("\\Assets\\Icons\\refresh.png", UriKind.Relative);
            _appBarButtons[2].Text = "refresh";
            _appBarButtons[2].IsEnabled = true;
            _appBarButtons[2].Click += Refresh_Click;

            ApplicationBar.Buttons.Clear();
            foreach (var button in _appBarButtons)
                ApplicationBar.Buttons.Add(button as IApplicationBarIconButton);
        }

        private void UpdateMenuItems()
        {
            if (_appBarButtons == null || ApplicationBar.Buttons.Count == 0)
                BuildAppBar();

            var vm = this.DataContext as MessagesViewModel;
            if (vm != null)
            {
                // TODO: If item in current pivot selected, enable reply/delete
                if (vm.SelectedItem != null)
                {
                    _appBarButtons[1].IsEnabled = true;
                }
                else
                {
                    _appBarButtons[1].IsEnabled = false;
                }
            }
        }

        private void pivot_LoadedPivotItem(object sender, PivotItemEventArgs e)
        {
            var vm = this.DataContext as MessagesViewModel;
            if (vm != null)
            {
                vm.SelectedItem = null;
            }
            UpdateMenuItems();
        }

        private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateMenuItems();
        }
	}
}