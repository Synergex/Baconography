using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
using SoldOutW8;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Markup;

namespace BaconographyW8.Converters
{
    public class MarkdownConverter : IValueConverter
    {
        object bindingContext;
        static int insertionLength = "<LineBreak/>".Length + "</Paragraph>".Length + "<Paragraph>".Length;
        unsafe public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (bindingContext == null)
            {
                bindingContext = new
                {
                    TextButtonStyle = App.Current.Resources["TextButtonStyle"] as Style,
                    BodyText = App.Current.Resources["BaconReadingBodyParagraphStyle"] as Style,
                    Locator = App.Current.Resources["Locator"] as ViewModelLocator,
                    StaticCommands = App.Current.Resources["StaticCommands"]
                };
            }

            if (value is MarkdownData)
            {
                var markdownData = value as MarkdownData;
                try
                {
                    if (markdownData.ProcessedMarkdownBlock.Count > 1)
                    {
                        var stackPanel = new StackPanel { Orientation = Orientation.Vertical };
                        foreach (var part in markdownData.ProcessedMarkdownBlock)
                        {
                            stackPanel.Children.Add(MakeMarkdown(part) as UIElement);
                        }
                        return stackPanel;
                    }
                    else
                        return MakeMarkdown(markdownData.ProcessedMarkdownBlock[0]);
                }
                catch
                {
                    return MakePlain(markdownData.ProcessedMarkdownBlock[0].Item3);
                }
            }
            else
                return new TextBlock { Text = "" };
        }

        private unsafe object MakeMarkdown(Tuple<bool, string, string> value)
        {
            if (value.Item1)
            {
                return MakePlain(value.Item3);
            }
            else
            {
                try
                {
                    var uiElement = XamlReader.Load(string.Format("<RichTextBlock xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:view=\"using:BaconographyW8.View\"><RichTextBlock.Blocks>{0}</RichTextBlock.Blocks></RichTextBlock>", value.Item2)) as RichTextBlock;
                    uiElement.DataContext = bindingContext;
                    return uiElement;
                }
                catch
                {
                    return MakePlain(value.Item3);
                }
            }
        }

        private object MakePlain(string value)
        {
            return new TextBlock { Text = value as string, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(10, 0, 0, 0) };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

    }
}
