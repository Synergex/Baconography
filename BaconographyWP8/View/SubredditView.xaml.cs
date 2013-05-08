using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace BaconographyWP8.View
{
	public partial class SubredditView : UserControl
	{
		public SubredditView()
		{
			InitializeComponent();
		}

		public static readonly DependencyProperty IsPinnedProperty =
			DependencyProperty.Register(
				"IsPinned",
				typeof(bool),
				typeof(SubredditView),
				new PropertyMetadata(false, OnIsPinnedChanged)
			);

		private static void OnIsPinnedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var subreddit = (SubredditView)d;
			subreddit.IsPinned = (bool)e.NewValue;
		}

		public bool IsPinned
		{
			get { return (bool)GetValue(IsPinnedProperty); }
			set { SetValue(IsPinnedProperty, value); }
		}
	}
}
