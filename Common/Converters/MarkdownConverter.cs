using Baconography.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;

namespace Baconography.Common.Converters
{
    public class MarkdownConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string)
            {
                try
                {
                    var uiElement = XamlReader.Load(string.Format("<RichTextBlock xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><RichTextBlock.Blocks>{0}</RichTextBlock.Blocks></RichTextBlock>", (new MarkdownSharp.Markdown()).Transform(value as string))) as RichTextBlock;
                    uiElement.DataContext = new { TextButtonStyle = App.Current.Resources["TextButtonStyle"] as Style, Locator = App.Current.Resources["Locator"] as ViewModelLocator };
                    return uiElement;
                }
                catch
                {
                    return new TextBlock { Text = value as string };
                }
            }
            else
                return new TextBlock { Text = "" };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
