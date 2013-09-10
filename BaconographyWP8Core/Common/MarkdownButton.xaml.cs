using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using BaconographyPortable.Services;
using System.Windows.Media;
using Microsoft.Practices.ServiceLocation;
using BaconographyPortable.Common;

namespace BaconographyWP8.Common
{
	public partial class MarkdownButton : Button
	{

		IOfflineService _offlineService;

		static SolidColorBrush history = new SolidColorBrush(Colors.Gray);
		static SolidColorBrush noHistory = new SolidColorBrush(Color.FromArgb(255, 218, 165, 32));

		public MarkdownButton(string url, object content)
		{
			InitializeComponent();
			_offlineService = ServiceLocator.Current.GetInstance<IOfflineService>();
			this.BorderThickness = new Thickness(0);
            Url = url;
            if (url.StartsWith("#") && url == ((string)content))
            {
                Text = "";
            }
            else
                Text = content as string;
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
				if (String.IsNullOrEmpty((string)GetValue(TextProperty)))
					SetValue(TextProperty, value);
			}
		}

		private static void OnUrlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var button = (MarkdownButton)d;
			button.Url = (string)e.NewValue;
		}

		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register(
				"Text",
				typeof(string),
				typeof(MarkdownButton),
				new PropertyMetadata(null, OnTextChanged)
			);

		public string Text
		{
			get
			{
				return (string)GetValue(TextProperty);
			}
			set
			{
				SetValue(TextProperty, value);
			}
		}

		private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var button = (MarkdownButton)d;
			button.Text = (string)e.NewValue;
		}

		protected override void OnClick()
		{
			UtilityCommandImpl.GotoLinkImpl(Url);
			base.OnClick();
		}
	}
}
