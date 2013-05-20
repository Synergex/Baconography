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
	public partial class PullToRefreshView : UserControl
	{
		public PullToRefreshView()
		{
			InitializeComponent();
		}

		public static readonly DependencyProperty IsPulledProperty =
			DependencyProperty.Register(
				"IsPulled",
				typeof(bool),
				typeof(PullToRefreshView),
				new PropertyMetadata(false, OnIsPulledPropertyChanged)
			);

		public bool IsPulled
		{
			get
			{
				return (bool)GetValue(IsPulledProperty);
			}
			set
			{
				SetValue(IsPulledProperty, value);
				if (value == true)
					VisualStateManager.GoToState(button, "Pulling", true);
				else
					VisualStateManager.GoToState(button, "NotPulling", true);
			}
		}

		private static void OnIsPulledPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var view = (PullToRefreshView)d;
			view.IsPulled = (bool)e.NewValue;
		}
	}
}
