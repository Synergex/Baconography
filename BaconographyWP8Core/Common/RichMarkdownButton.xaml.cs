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
	public partial class RichMarkdownButton : Button
	{

		IOfflineService _offlineService;

		static SolidColorBrush history = new SolidColorBrush(Colors.Gray);
		static SolidColorBrush noHistory = new SolidColorBrush(Color.FromArgb(255, 218, 165, 32));

        public RichMarkdownButton(string url, object content)
		{
			InitializeComponent();
			_offlineService = ServiceLocator.Current.GetInstance<IOfflineService>();
			this.BorderThickness = new Thickness(0);
            Url = url;
            RealContent = content as UIElement;
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
				if (String.IsNullOrEmpty((string)GetValue(RealContentProperty)))
					SetValue(RealContentProperty, value);
			}
		}

		private static void OnUrlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var button = (MarkdownButton)d;
			button.Url = (string)e.NewValue;
		}

		public static readonly DependencyProperty RealContentProperty =
			DependencyProperty.Register(
                "RealContent",
                typeof(UIElement),
				typeof(MarkdownButton),
                new PropertyMetadata(null)
			);

		public UIElement RealContent
		{
			get
			{
                return (UIElement)GetValue(RealContentProperty);
			}
			set
			{
				SetValue(RealContentProperty, value);
			}
		}

		protected override void OnClick()
		{
			UtilityCommandImpl.GotoLinkImpl(Url);
			base.OnClick();
		}
	}
}
