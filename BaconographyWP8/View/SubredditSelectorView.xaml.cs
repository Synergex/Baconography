using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using BaconographyPortable.ViewModel;

namespace BaconographyWP8.View
{
	public partial class SubredditSelectorView : UserControl
	{
		public SubredditSelectorView()
		{
			this.InitializeComponent();
		}

		private void manualBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
		{
			if (e.Key == System.Windows.Input.Key.Enter)
			{
				this.Focus();
				var ssvm = this.DataContext as SubredditSelectorViewModel;
				if (ssvm != null)
					ssvm.PinSubreddit.Execute(ssvm);
			}
		}
	}
}
