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

            if (!string.IsNullOrWhiteSpace(value as string))
            {
                try
                {
                    var text = value as string;
                    if (text.Length > 2048)
                    {
                        var stackPanel = new StackPanel { Orientation = Orientation.Vertical };
                        foreach (var part in SplitText(text))
                        {
                            stackPanel.Children.Add(MakeMarkdown(part) as UIElement);
                        }
                        return stackPanel;
                    }
                    else
                        return MakeMarkdown(value);
                }
                catch
                {
                    return MakePlain(value);
                }
            }
            else
                return new TextBlock { Text = "" };
        }

        private unsafe object MakeMarkdown(object value)
        {
            var startingText = value as string;
            string markdown = null;
            fixed (char* textPtr = startingText)
            {
                var markdownPtr = SoldOut.MarkdownToXaml((uint)textPtr, (uint)startingText.Length);
                if (markdownPtr != 0)
                    markdown = new string((char*)markdownPtr);
            }


            //bad markdown (possibly due to unicode char, just pass it through plain)
            var noWhiteStartingText = startingText.Replace(" ", "").Replace("\n", "");
            var noWhiteMarkdown = markdown.Replace(" ", "").Replace("\n", "").Replace("<paragraph>", "");
            var isSame = (markdown.Length < "<paragraph></paragraph>".Length) || string.Compare(noWhiteStartingText, 0, noWhiteMarkdown, 0, noWhiteStartingText.Length) == 0;

            if (isSame)
            {
                return MakePlain(value);
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

                //for (int lineBreakPos = markdown.IndexOf("<LineBreak/>", 0); lineBreakPos != -1 && lineBreakPos + "<LineBreak/>".Length + 1 < markdown.Length; lineBreakPos = markdown.IndexOf("<LineBreak/>", lineBreakPos + insertionLength))
                //{
                //    //unfortnately the renderer doesnt really allow us to  wrap this in a paragrpah properly (For xaml)
                //    if (lineBreakPos > -1)
                //    {
                //        var paragraphEnding = markdown.LastIndexOf("</Paragraph>", lineBreakPos);
                //        if (paragraphEnding != -1)
                //        {
                //            markdown = markdown.Insert(paragraphEnding + "</Paragraph>".Length, "<Paragraph>").Insert(lineBreakPos + "<Paragraph>".Length + "<LineBreak/>".Length, "</Paragraph>");
                //        }
                //    }
                //}
                //xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:view=\"clr-namespace:BaconographyWP8.View\"

                //var markdown2 = "<Paragraph>Reminds me of <InlineUIContainer><Button><Button.Content>this</Button.Content></Button></InlineUIContainer></Paragraph>";

                if (!markdown.Contains("<Paragraph>"))
                {
                    markdown = "<Paragraph>" + markdown + "</Paragraph>";
                }

                var uiElement = XamlReader.Load(string.Format("<RichTextBox xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:common=\"clr-namespace:BaconographyWP8.Common;assembly=BaconographyWP8Core\">{0}</RichTextBox>", markdown)) as RichTextBox;
                uiElement.DataContext = bindingContext;
                return uiElement;
            }
        }

        private List<string> SplitText(string semiCleanText)
        {
            List<string> textBlocks = new List<string>();
            int startIndex = 0;
            int foundIndex = semiCleanText.IndexOf('\n', 1024);
            while (startIndex < semiCleanText.Length && foundIndex != -1)
            {
                textBlocks.Add(semiCleanText.Substring(startIndex, (foundIndex - startIndex) - 1));
                startIndex = foundIndex + 1;
                if ((startIndex + 1024) > (semiCleanText.Length - 1))
                    break;
                foundIndex = semiCleanText.IndexOf('\n', startIndex + 1024);
            }

            if (startIndex < (semiCleanText.Length - 1))
            {
                textBlocks.Add(semiCleanText.Substring(startIndex));
            }
            
            return textBlocks;
            
        }

        private object MakePlain(object value)
        {
            var semiCleanText = value as string;
            if (semiCleanText != null)
                semiCleanText = semiCleanText.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&apos;", "'");

            //this is an awful hack to decide if we need to split
            if (semiCleanText.Length > 1024)
            {
                var stackPanel = new StackPanel { Orientation = Orientation.Vertical };
                foreach (var part in SplitText(semiCleanText))
                {
                    stackPanel.Children.Add(new TextBlock { Text = part, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(10,0,0,0) });
                }
                return stackPanel;
            }
            else
            {
				return new TextBlock { Text = semiCleanText, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(10,0,0,0) };
            }
        }

       
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
