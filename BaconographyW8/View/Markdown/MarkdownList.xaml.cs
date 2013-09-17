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
    public sealed partial class MarkdownList : UserControl
    {
        public MarkdownList(bool numbered, IEnumerable<UIElement> elements)
        {
            this.InitializeComponent();
            int number = 1;
            foreach (var element in elements)
            {
                var itemPanel = new StackPanel();
                itemPanel.Orientation = Orientation.Horizontal;
                itemPanel.Children.Add(new TextBlock { Margin = new Thickness(5, 0, 15, 0), Text = numbered ? (number++).ToString() + "." : "&#x25CF;" });
                itemPanel.Children.Add(element);
                items.Children.Add(itemPanel);
            }
        }
    }
}
