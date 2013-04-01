
using BaconographyPortable.ViewModel;
using BaconographyWP8Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BaconographyWP8.View
{
	[ViewUri("/View/LinkView.xaml")]
    public sealed partial class LinkView : UserControl
    {
        public LinkView()
        {
			this.InitializeComponent();
        }

		private void TitleButton_Hold(object sender, System.Windows.Input.GestureEventArgs e)
		{
			var vm = this.DataContext as LinkViewModel;
			if (!vm.InComments)
				vm.IsExtendedOptionsShown = !vm.IsExtendedOptionsShown;
		}

		private void TitleButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			var vm = this.DataContext as LinkViewModel;
			if (!vm.InComments)
				vm.GotoComments();
		}

		public static readonly DependencyProperty DisplaySubredditProperty =
			DependencyProperty.Register(
				"DisplaySubreddit",
				typeof(bool),
				typeof(LinkView),
				new PropertyMetadata(false, OnDisplaySubredditChanged)
			);

		private static void OnDisplaySubredditChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var link = (LinkView)d;
			link.DisplaySubreddit = (bool)e.NewValue;
		}

		public bool DisplaySubreddit
		{
			get { return (bool)GetValue(DisplaySubredditProperty); }
			set { SetValue(DisplaySubredditProperty, value); }
		}

    }
}
