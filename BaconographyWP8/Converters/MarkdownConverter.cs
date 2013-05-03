﻿using BaconographyPortable.ViewModel;
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
                        var rtb = new RichTextBox();
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
                        //xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:view=\"clr-namespace:BaconographyWP8.View\"

                        //var markdown2 = "<Paragraph>Reminds me of <InlineUIContainer><Button><Button.Content>this</Button.Content></Button></InlineUIContainer></Paragraph>";

                        if (!markdown.Contains("<Paragraph>"))
                        {
                            markdown = "<Paragraph>" + markdown + "</Paragraph>";
                        }

						var uiElement = XamlReader.Load(string.Format("<RichTextBox xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" xmlns:common=\"clr-namespace:BaconographyWP8.Common;assembly=BaconographyWP8\">{0}</RichTextBox>", markdown)) as RichTextBox;
                        uiElement.DataContext = bindingContext;
                        return uiElement;
                    }
                }
                catch
                {
                    var semiCleanText = value as string;
                    if (semiCleanText != null)
                        semiCleanText = semiCleanText.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&apos;", "'");
                    return new TextBlock { Text = semiCleanText, TextWrapping = TextWrapping.Wrap };
                }
            }
            else
                return new TextBlock { Text = "" };
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
