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
            int rowCount = 0;
            foreach (var element in elements)
            {
                theGrid.RowDefinitions.Add(new RowDefinition());
                var text = new TextBlock { TextWrapping = System.Windows.TextWrapping.Wrap, Margin = new Thickness(5, 0, 15, 0), Text = numbered ? (number++).ToString() + "." : "\u25CF" };
                text.SetValue(Grid.RowProperty, rowCount);
                text.SetValue(Grid.ColumnProperty, 0);
                element.SetValue(Grid.RowProperty, rowCount++);
                element.SetValue(Grid.ColumnProperty, 1);
                theGrid.Children.Add(text);
                theGrid.Children.Add(element);
            }
        }
    }
}
