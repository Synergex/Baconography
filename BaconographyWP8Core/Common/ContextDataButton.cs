using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Controls;

namespace BaconographyWP8.Common
{
	public class ContextDataButton : Button
	{
		public ContextDataButton()
		{

		}

		public static readonly DependencyProperty ContextDataProperty =
			DependencyProperty.Register(
				"ContextData",
				typeof(object),
				typeof(ContextDataButton),
				new PropertyMetadata(false, OnContextDataChanged)
			);

		private static void OnContextDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var cd = (ContextDataButton)d;
			cd.ContextData = e.NewValue;
		}

		public object ContextData
		{
			get { return GetValue(ContextDataProperty); }
			set { SetValue(ContextDataProperty, value); }
		}
	}
}
