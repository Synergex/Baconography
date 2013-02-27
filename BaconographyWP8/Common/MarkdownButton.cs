using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace BaconographyWP8.Common
{
	class MarkdownButton : Button
	{
		public static readonly DependencyProperty UrlProperty =
			DependencyProperty.Register(
				"Url",
				typeof(object),
				typeof(FixedLongListSelector),
				new PropertyMetadata(null, OnUrlChanged)
			);

		public object Url
		{
			get { return GetValue(UrlProperty); }
			set { SetValue(UrlProperty, value); }
		}

		private static void OnUrlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var button = (MarkdownButton)d;
			button.Url = e.NewValue;
		}
	}
}
