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
    public partial class MarkdownQuote : UserControl
    {
        public MarkdownQuote(string contents)
        {
            this.InitializeComponent();
            content.Content = new TextBlock {TextWrapping = System.Windows.TextWrapping.Wrap, Text = contents };
        }

        public MarkdownQuote(UIElement contents)
        {
            this.InitializeComponent();
            content.Content = contents;
        }
    }
}
