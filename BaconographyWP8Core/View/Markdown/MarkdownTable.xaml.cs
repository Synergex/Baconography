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
    public partial class MarkdownTable : UserControl
    {
        public MarkdownTable(IEnumerable<UIElement> headers, IEnumerable<IEnumerable<UIElement>> body)
        {
            InitializeComponent();
            int x = 0, y = 0;
            theGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            int maxX = headers.Count() - 1;
            foreach (var header in headers)
            {
                theGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                header.SetValue(Grid.ColumnProperty, x);
                header.SetValue(Grid.RowProperty, y);
                
                theGrid.Children.Add(header);
                x++;
            }

            foreach (var row in body)
            {
                theGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                x = 0;
                y++;
                foreach (var column in row)
                {
                    column.SetValue(Grid.ColumnProperty, x);
                    column.SetValue(Grid.RowProperty, y);
                    theGrid.Children.Add(column);
                    x++;
                }

            }
        }
    }
}
