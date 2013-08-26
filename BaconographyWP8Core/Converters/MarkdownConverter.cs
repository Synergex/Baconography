using BaconographyPortable.ViewModel;
using SoldOutWP8;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.IO;
using BaconographyWP8Core.Common;
using BaconographyPortable.Services;

namespace BaconographyWP8.Converters
{
    public class MarkdownConverter : IValueConverter
    {
        object bindingContext;
        static int insertionLength = "<LineBreak/>".Length + "</Paragraph>".Length + "<Paragraph>".Length;
        unsafe public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (bindingContext == null)
            {
                bindingContext = new
                        {
                            TextButtonStyle = Styles.Resources["TextButtonStyle"] as Style,
                            BodyText = Styles.Resources["BaconReadingBodyParagraphStyle"] as Style,
                            Locator = Styles.Resources["Locator"] as ViewModelLocator,
                            StaticCommands = Styles.Resources["StaticCommands"]
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
                    var uiElement = XamlReader.Load(string.Format("<RichTextBox xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:common=\"clr-namespace:BaconographyWP8.Common;assembly=BaconographyWP8Core\">{0}</RichTextBox>", value.Item2)) as RichTextBox;
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

       
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
