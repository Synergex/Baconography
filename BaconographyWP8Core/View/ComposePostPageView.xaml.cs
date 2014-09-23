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
using Microsoft.Practices.ServiceLocation;
using BaconographyPortable.ViewModel;
using BaconographyPortable.Services;
using System.Windows.Data;

namespace BaconographyWP8.View
{
    [ViewUri("/BaconographyWP8Core;component/View/ComposePostPageView.xaml")]
    public partial class ComposePostPageView : PhoneApplicationPage
    {
        public ComposePostPageView()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {   
            if (e.NavigationMode == System.Windows.Navigation.NavigationMode.Back)
            {
                var vm = this.DataContext as ComposePostViewModel;
                if (vm != null)
                {
                    
                    vm.RefreshUser.Execute(null);
                }
            }
            else if (e.NavigationMode == NavigationMode.New)
            {
                var vm = this.DataContext as ComposePostViewModel;
                if (vm.Editing)
                {
                    pivot.Title = "BACONOGRAPHY > EDIT POST";
                    TitleBox.SetValue(Grid.RowProperty, 0);
                    TextInput.SetValue(Grid.RowProperty, 1);
                    TextInput.SetValue(Grid.RowSpanProperty, 3);
                }
                else
                {
                    pivot.Title = "BACONOGRAPHY > SUBMIT POST";
                    TitleBox.SetValue(Grid.RowProperty, 2);
                    TextInput.SetValue(Grid.RowProperty, 3);
                    TextInput.SetValue(Grid.RowSpanProperty, 1);
                }
            }
            UpdateMenuItems();
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.New && e.Uri.ToString() == "/BaconographyWP8Core;component/MainPage.xaml" && e.IsCancelable)
                e.Cancel = true;
            else
                base.OnNavigatingFrom(e);
        }

        private void Send_Click(object sender, EventArgs e)
        {
            BindingExpression bindingExpression = TextInputBox.GetBindingExpression(TextBox.TextProperty);
            if (bindingExpression != null)
            {
                bindingExpression.UpdateSource();
            }

            

            var vm = this.DataContext as ComposePostViewModel;
            if (vm != null)
            {
                var pivotItem = pivot.SelectedItem as PivotItem;
                if (pivotItem != null)
                {
                    vm.Kind = pivotItem.Header as string;
                }
                vm.Submit.Execute(null);
            }
        }

        private void ChangeUser_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as ComposePostViewModel;
            if (vm != null)
            {
                var _navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
                _navigationService.Navigate<LoginPageView>(null);
            }
        }

        private void Cancel_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Cancel this new post?", "confirm", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                var _navigationService = ServiceLocator.Current.GetInstance<INavigationService>();
                _navigationService.GoBack();
            }
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

            var vm = this.DataContext as ComposePostViewModel;
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

        private void pivot_LoadedPivotItem(object sender, PivotItemEventArgs e)
        {
            var vm = this.DataContext as ComposePostViewModel;
            if (vm == null)
                return;
        }

        private void TextInputBox_TextChanged(object sender, TextChangedEventArgs e)
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