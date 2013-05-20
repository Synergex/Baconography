﻿
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
			if (!InComments)
				vm.IsExtendedOptionsShown = !vm.IsExtendedOptionsShown;
		}

		private void TitleButton_Tap(object sender, System.Windows.Input.GestureEventArgs e)
		{
			var vm = this.DataContext as LinkViewModel;
			if (!InComments)
				vm.GotoComments();
		}

		public static readonly DependencyProperty InCommentsProperty =
			DependencyProperty.Register(
				"InComments",
				typeof(bool),
				typeof(LinkView),
				new PropertyMetadata(false, OnInCommentsChanged)
			);

		private static void OnInCommentsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var link = (LinkView)d;
			link.InComments = (bool)e.NewValue;
		}

		public bool InComments
		{
			get { return (bool)GetValue(InCommentsProperty); }
			set { SetValue(InCommentsProperty, value); }
		}

        private void Link_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var vm = this.DataContext as LinkViewModel;
			if (vm != null)
                vm.GotoLink.Execute(vm);
        }
    }
}
