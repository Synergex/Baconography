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
        public object Convert(object value, Type targetType, object parameter, string language)
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

            if (!string.IsNullOrWhiteSpace(value as string))
            {
                try
                {
                    var startingText = value as string;
                    var markdown = SoldOut.MarkdownToXaml(startingText);

                    var isSame = string.Compare(startingText, 0, markdown, "<paragraph>".Length, startingText.Length) == 0;

                    if (isSame)
                    {
                        var rtb = new RichTextBlock();
                        var pp = new Paragraph();
                        pp.Inlines.Add(new Run { Text = startingText } );
                        rtb.Blocks.Add(pp);
                        return rtb;
                    }
                    else
                    {
                        var uiElement = XamlReader.Load(string.Format("<RichTextBlock xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"><RichTextBlock.Blocks>{0}</RichTextBlock.Blocks></RichTextBlock>", markdown)) as RichTextBlock;
                        uiElement.DataContext = bindingContext;
                        return uiElement;
                    }
                }
                catch
                {
                    var rtb = new RichTextBlock();
                    var pp = new Paragraph();
                    pp.Inlines.Add(new Run { Text = value as string });
                    rtb.Blocks.Add(pp);
                    return rtb;
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
