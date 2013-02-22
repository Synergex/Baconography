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

                    //bad markdown (possibly due to unicode char, just pass it through plain)
                    var isSame = (markdown.Length < "<paragraph></paragraph>".Length) || string.Compare(startingText, 0, markdown, "<paragraph>\n".Length, startingText.Length) == 0;

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
                        markdown = markdown.Trim('\n');
                        if (!markdown.EndsWith("</Paragraph>"))
                        {
                            var lastParagraph = markdown.LastIndexOf("</Paragraph>");
                            if (lastParagraph != -1)
                            {
                                markdown = markdown.Substring(0, lastParagraph + "</Paragraph>".Length) + "<Paragraph>" + markdown.Substring(lastParagraph + "</Paragraph>".Length + 1) + "</Paragraph>";
                            }
                        }

                        for (int lineBreakPos = markdown.IndexOf("<LineBreak/>", 0); lineBreakPos != -1 && lineBreakPos + "<LineBreak/>".Length + 1 < markdown.Length; lineBreakPos = markdown.IndexOf("<LineBreak/>", lineBreakPos + insertionLength))
                        {
                            //unfortnately the renderer doesnt really allow us to  wrap this in a paragrpah properly (For xaml)
                            if (lineBreakPos > -1)
                            {
                                var paragraphEnding = markdown.LastIndexOf("</Paragraph>", lineBreakPos);
                                if (paragraphEnding != -1)
                                {
                                    markdown = markdown.Insert(paragraphEnding + "</Paragraph>".Length, "<Paragraph>").Insert(lineBreakPos + "<Paragraph>".Length + "<LineBreak/>".Length, "</Paragraph>");
                                }
                            }
                        }
                        var uiElement = XamlReader.Load(string.Format("<RichTextBlock xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:view=\"using:BaconographyW8.View\"><RichTextBlock.Blocks>{0}</RichTextBlock.Blocks></RichTextBlock>", markdown)) as RichTextBlock;
                        uiElement.DataContext = bindingContext;
                        return uiElement;
                    }
                }
                catch
                {
                    var rtb = new RichTextBlock();
                    var pp = new Paragraph();

                    var semiCleanText = value as string;
                    if(semiCleanText != null)
                        semiCleanText = semiCleanText.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&apos;", "'");

                    pp.Inlines.Add(new Run { Text = semiCleanText });
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
