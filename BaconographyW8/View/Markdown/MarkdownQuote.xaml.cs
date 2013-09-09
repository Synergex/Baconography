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
    public sealed partial class MarkdownQuote : UserControl
    {
        public MarkdownQuote(string contents)
        {
            this.InitializeComponent();
            Content = new TextBlock { Text = contents };
        }

        public MarkdownQuote(UIElement contents)
        {
            this.InitializeComponent();
            Content = contents;
        }
    }
}
