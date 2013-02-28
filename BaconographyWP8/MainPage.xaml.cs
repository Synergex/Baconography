using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using BaconographyWP8.Resources;
using BaconographyPortable.Services;
using Microsoft.Practices.ServiceLocation;
using BaconographyPortable.ViewModel;
using Newtonsoft.Json;

namespace BaconographyWP8
{
    public partial class MainPage : PhoneApplicationPage
    {
		bool isNewInstance;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
			isNewInstance = true;
            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			if (this.State != null && this.State.ContainsKey("SelectedCommentTreeMessage"))
			{
				
				
				//_selectedCommentTree = this.State["SelectedCommentTreeMessage"] as SelectCommentTreeMessage;
				//Messenger.Default.Send<SelectCommentTreeMessage>(_selectedCommentTree);
			}
			if (isNewInstance)
			{
				var mpvm = ServiceLocator.Current.GetInstance<MainPageViewModel>() as MainPageViewModel;
				mpvm.LoadSubreddits();
				isNewInstance = false;
			}
		}


		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			if (e.NavigationMode != NavigationMode.Back)
			{
				var mpvm = ServiceLocator.Current.GetInstance<MainPageViewModel>() as MainPageViewModel;
				mpvm.SaveSubreddits();
			}
			//this.State["SelectedCommentTreeMessage"] = _selectedCommentTree;
		}

        // Sample code for building a localized ApplicationBar
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Set the page's ApplicationBar to a new instance of ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Create a new button and set the text value to the localized string from AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Create a new menu item with the localized string from AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
    }
}