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
	public partial class HelpView : UserControl
	{
        public HelpView()
		{
			InitializeComponent();
		}

        public static readonly DependencyProperty TopicProperty =
            DependencyProperty.Register(
                "Topic",
                typeof(string),
                typeof(HelpView),
                new PropertyMetadata("", OnTopicPropertyChanged)
            );

        public string Topic
        {
            get
            {
                return (string)GetValue(TopicProperty);
            }
            set
            {
                SetValue(TopicProperty, value);
            }
        }

        private static void OnTopicPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (HelpView)d;
            view.Topic = (string)e.NewValue;
        }

        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register(
                "Content",
                typeof(string),
                typeof(HelpView),
                new PropertyMetadata("", OnContentPropertyChanged)
            );

        public string Content
        {
            get
            {
                return (string)GetValue(ContentProperty);
            }
            set
            {
                SetValue(ContentProperty, value);
            }
        }

        private static void OnContentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var view = (HelpView)d;
            view.Content = (string)e.NewValue;
        }

	}
}
