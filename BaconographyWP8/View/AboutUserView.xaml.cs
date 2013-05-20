using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using BaconographyPortable.Messages;
using GalaSoft.MvvmLight.Messaging;
using Newtonsoft.Json;
using BaconographyPortable.ViewModel;
using BaconographyWP8Core;
using BaconographyWP8.Common;

namespace BaconographyWP8.View
{
	[ViewUri("/View/AboutUserView.xaml")]
	public partial class AboutUserView : PhoneApplicationPage
	{

		int _offsetKnob = 7;
		object lastItem;

		public AboutUserView()
		{
			InitializeComponent();
		}

		void linksView_ItemRealized(object sender, ItemRealizationEventArgs e)
		{
			lastItem = e.Container.Content;
			var linksView = sender as FixedLongListSelector;
			if (linksView.ItemsSource != null && linksView.ItemsSource.Count >= _offsetKnob)
			{
				if (e.ItemKind == LongListSelectorItemKind.Item)
				{
					if ((e.Container.Content).Equals(linksView.ItemsSource[linksView.ItemsSource.Count - _offsetKnob]))
					{
						var viewModel = DataContext as AboutUserViewModel;
                        if (viewModel != null && viewModel.Things.HasMoreItems)
                        {
                            viewModel.Things.LoadMoreItemsAsync(30);
                        }
					}
				}
			}
		}

		SelectUserAccountMessage _selected;

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			if (e.NavigationMode == NavigationMode.Back)
			{

			}
			else
			{
				if (this.State != null && this.State.ContainsKey("SelectedUserAccountMessage"))
				{
					_selected = this.State["SelectedUserAccountMessage"] as SelectUserAccountMessage;
					Messenger.Default.Send<SelectUserAccountMessage>(_selected);
				}
				else if (this.NavigationContext.QueryString["data"] != null)
				{
					var unescapedData = HttpUtility.UrlDecode(this.NavigationContext.QueryString["data"]);
					var deserializedObject = JsonConvert.DeserializeObject<SelectUserAccountMessage>(unescapedData);
					if (deserializedObject is SelectUserAccountMessage)
					{
						_selected = deserializedObject as SelectUserAccountMessage;
						Messenger.Default.Send<SelectUserAccountMessage>(_selected);
					}
				}

			}
		}


		protected override void OnNavigatedFrom(NavigationEventArgs e)
		{
			if (e.NavigationMode != NavigationMode.Back)
			{

			}
			else
			{
				this.State["SelectedUserAccountMessage"] = _selected;
				Content = null;
				if (DataContext != null)
					((AboutUserViewModel)DataContext).Cleanup();
			}
		}
	}
}