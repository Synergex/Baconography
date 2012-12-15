using BaconographyPortable.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;

namespace BaconographyW8.Converters
{
    public class MarkdownConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (!string.IsNullOrWhiteSpace(value as string))
            {
                try
                {
                    var startingText = value as string;
                    var markdown =  (new MarkdownSharp.Markdown()).Transform(startingText);

                    var isSame = string.Compare(startingText, 0, markdown, "<paragraph>".Length, startingText.Length) == 0;

                    if(isSame)
                    {
                        return new TextBlock { Text = startingText, TextWrapping = TextWrapping.Wrap };
                    }
                    else
                    {
                        var uiElement = XamlReader.Load(string.Format("<RichTextBlock xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><RichTextBlock.Blocks>{0}</RichTextBlock.Blocks></RichTextBlock>", markdown)) as RichTextBlock;
                        uiElement.DataContext = new 
                        { 
                            TextButtonStyle = App.Current.Resources["TextButtonStyle"] as Style, 
                            Locator = App.Current.Resources["Locator"] as ViewModelLocator,
                            StaticCommands = App.Current.Resources["StaticCommands"]
                        };
                        return uiElement;
                    }
                }
                catch
                {
                    return new TextBlock { Text = value as string, TextWrapping = TextWrapping.Wrap };
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
