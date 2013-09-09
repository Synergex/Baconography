using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace BaconographyWP8Core.View.Markdown
{
    public partial class MarkdownList : UserControl
    {
        public MarkdownList(bool numbered, IEnumerable<UIElement> elements)
        {
            InitializeComponent();
            int number = 1;
            foreach (var element in elements)
            {
                var itemPanel = new StackPanel();
                itemPanel.Orientation = Orientation.Horizontal;
                var text = new TextBlock { Margin = new Thickness(5, 0, 15, 0), Text = numbered ? (number++).ToString() + "." : "\u25CF" };
                itemPanel.Children.Add(text);
                itemPanel.Children.Add(element);
                items.Children.Add(itemPanel);
            }
        }
    }
}
