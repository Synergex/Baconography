using BaconographyPortable.Services;
using GalaSoft.MvvmLight.Ioc;
using Microsoft.Practices.ServiceLocation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace BaconographyWP8.Common
{
	public class MarkdownButton : Button
	{
		IOfflineService _offlineService;

		static SolidColorBrush history = new SolidColorBrush(Colors.Gray);
		static Brush noHistory;

		static MarkdownButton()
		{
			noHistory = App.Current.Resources["ApplicationForegroundThemeBrush"] as Brush;
		}

		public MarkdownButton()
		{
			_offlineService = ServiceLocator.Current.GetInstance<IOfflineService>();
		}

		public static readonly DependencyProperty UrlProperty =
			DependencyProperty.Register(
				"Url",
				typeof(string),
				typeof(MarkdownButton),
				new PropertyMetadata(null, OnUrlChanged)
			);

		public string Url
		{
			get { return (string)GetValue(UrlProperty); }
			set
			{
				if (_offlineService.HasHistory(value))
					this.Foreground = history;
				else
					this.Foreground = noHistory;
				SetValue(UrlProperty, value);
			}
		}

		private static void OnUrlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var button = (MarkdownButton)d;
			button.Url = (string)e.NewValue;
		}
	}
}
