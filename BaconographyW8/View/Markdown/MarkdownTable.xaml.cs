using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BaconographyW8.View.Markdown
{
    public sealed partial class MarkdownTable : UserControl
    {
        public MarkdownTable(IEnumerable<UIElement> headers, IEnumerable<IEnumerable<UIElement>> body)
        {
            this.InitializeComponent();
            int x = 0, y = 0;
            theGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
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
