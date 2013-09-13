using BaconographyPortable.ViewModel;
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
using SnuDomWP8;
using System.Windows.Media;
using BaconographyWP8.Common;
using BaconographyWP8Core.View.Markdown;
using GalaSoft.MvvmLight.Command;
using BaconographyPortable.Common;

namespace BaconographyWP8.Converters
{
    public class MarkdownConverter : IValueConverter
    {
        unsafe public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is MarkdownData)
            {
                var markdownData = value as MarkdownData;
                try
                {
                    var categoryVisitor = new SnuDomCategoryVisitor();
                    ((IDomObject)markdownData.MarkdownDom).Accept(categoryVisitor);
                    switch (categoryVisitor.Category)
                    {
                        case MarkdownCategory.PlainText:
                            {
                                var visitor = new SnuDomPlainTextVisitor();
                                ((IDomObject)markdownData.MarkdownDom).Accept(visitor);
                                return MakePlain(visitor.Result);
                            }
                        case MarkdownCategory.Formatted:
                        case MarkdownCategory.Full:
                            {
                                var visitor = new SnuDomFullUIVisitor(Styles.Resources["PhoneForegroundBrush"] as Brush);
                                ((IDomObject)markdownData.MarkdownDom).Accept(visitor);
                                if (visitor.ResultGroup != null)
                                    return visitor.ResultGroup;
                                else
                                    return visitor.Result;
                            }
                        default:
                            return new TextBlock { Text = "" };
                    }
                    
                }
                catch(Exception ex)
                {
                    return MakePlain(ex.ToString());
                }
            }
            else
                return new TextBlock { Text = "" };
        }

        private object MakePlain(string value)
        {
            return new TextBlock { Text = value as string, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 6, 4, 6) };
        }

       
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    class SnuDomFullUIVisitor : IDomVisitor
    {
        public SnuDomFullUIVisitor(Brush forgroundBrush)
        {
            _forgroundBrush = forgroundBrush;
        }
        Brush _forgroundBrush;
        private int _textLengthInCurrent = 0;
        public RichTextBox Result = new RichTextBox { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(-6, 6, 4, 6) };
        public StackPanel ResultGroup = null;
        System.Windows.Documents.Paragraph _currentParagraph;

        private void MaybeSplitForParagraph()
        {
            if (_textLengthInCurrent > 1000)
            {
                if (ResultGroup == null)
                {
                    ResultGroup = new StackPanel { Orientation = Orientation.Vertical};
                    ResultGroup.Children.Add(Result);
                    Result.Margin = new Thickness(-6);
                }

                ResultGroup.Children.Add(Result = new RichTextBox { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(-6, 6, 4, 6) });
                _textLengthInCurrent = 0;
            }

            _currentParagraph = new System.Windows.Documents.Paragraph { TextAlignment = TextAlignment.Left };
            Result.Blocks.Add(_currentParagraph);
        }

        private void DirectlyPlaceUIContent(UIElement element)
        {
            if (ResultGroup == null)
            {
                ResultGroup = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0)};
                if (Result.Blocks.Count == 0)
                {
                    //nothing here yet so lets just ignore the current result and move on
                }
                else
                {
                    ResultGroup.Children.Add(Result);
                }
            }
            else if(ResultGroup.Children.Last() is RichTextBox && ((RichTextBox)ResultGroup.Children.Last()).Blocks.Count == 0)
            {
                ResultGroup.Children.Remove(ResultGroup.Children.Last());
            }
            ResultGroup.Children.Add(element);

            ResultGroup.Children.Add(Result = new RichTextBox { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(-6, 6, 4, 6) });
            _textLengthInCurrent = 0;
        }

        public void Visit(Text text)
        {
            var madeRun = new Run { Text = text.Contents };
            _textLengthInCurrent += text.Contents.Length;

            if (text.Italic)
                madeRun.FontStyle = FontStyles.Italic;

            if (text.Bold)
                madeRun.FontWeight = FontWeights.Bold;


            if (text.HeaderSize != 0)
            {
                switch (text.HeaderSize)
                {
                    case 1:
                        madeRun.FontSize = 24;
                        break;
                    case 2:
                        madeRun.FontSize = 24;
                        madeRun.FontWeight = FontWeights.Bold;
                        madeRun.Foreground = _forgroundBrush; 
                        break;
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                        madeRun.FontSize = 28;
                        madeRun.FontWeight = FontWeights.Bold;
                        break;
                }
                MaybeSplitForParagraph();
                _currentParagraph.Inlines.Add(madeRun);
                if (text.HeaderSize == 1)
                {
                    var inlineContainer = new System.Windows.Documents.InlineUIContainer();
                    inlineContainer.Child = new Border
                    {
                        Margin = new Thickness(0, 5, 0, 5),
                        Height = 1,
                        VerticalAlignment = System.Windows.VerticalAlignment.Top,
                        BorderBrush = _forgroundBrush,
                        BorderThickness = new Thickness(1),
                        MinWidth = 1800
                    };
                    _currentParagraph.Inlines.Add(inlineContainer);
                }
                else
                    _currentParagraph.Inlines.Add(new System.Windows.Documents.LineBreak());
                
            }
            else
            {
                if (_currentParagraph == null)
                {
                    _currentParagraph = new System.Windows.Documents.Paragraph { TextAlignment = TextAlignment.Left };
                    Result.Blocks.Add(_currentParagraph);
                }
                _currentParagraph.Inlines.Add(madeRun);
            }
        }

        public void Visit(SnuDomWP8.Paragraph paragraph)
        {
            MaybeSplitForParagraph();
            foreach (var elem in paragraph)
            {
                elem.Accept(this);
            }

        }

        public void Visit(HorizontalRule horizontalRule)
        {
            var inlineContainer = new System.Windows.Documents.InlineUIContainer();
            inlineContainer.Child = new Border
            {
                Margin = new Thickness(0, 5, 0, 5),
                Height = 2,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                BorderBrush = _forgroundBrush,
                BorderThickness = new Thickness(2),
                MinWidth = 1800
            };
            MaybeSplitForParagraph();
            _currentParagraph.Inlines.Add(inlineContainer);
        }

        public void Visit(SnuDomWP8.LineBreak lineBreak)
        {
            _currentParagraph.Inlines.Add(new System.Windows.Documents.LineBreak());
        }

        public void Visit(Link link)
        {
            Inline inlineContainer = null;
            SnuDomCategoryVisitor categoryVisitor = new SnuDomCategoryVisitor();
            if (link.Display != null)
            {
                foreach (var item in link.Display)
                {
                    item.Accept(categoryVisitor);
                }
            }

            if (categoryVisitor.Category == MarkdownCategory.PlainText)
            {
                var plainTextVisitor = new SnuDomPlainTextVisitor();
                if (link.Display != null && link.Display.FirstOrDefault() != null)
                {
                    foreach (var item in link.Display)
                        item.Accept(plainTextVisitor);
                }
                else
                    plainTextVisitor.Result = link.Url;

                inlineContainer = new Hyperlink { Command = new RelayCommand<string>(UtilityCommandImpl.GotoLinkImpl), CommandParameter = link.Url };
                ((Hyperlink)inlineContainer).Inlines.Add(plainTextVisitor.Result);
                //inlineContainer.Child = new MarkdownButton(link.Url, plainTextVisitor.Result);
            }
            else
            {
                inlineContainer = new Hyperlink { Command = new RelayCommand<string>(UtilityCommandImpl.GotoLinkImpl), CommandParameter = link.Url };
                var text = link.Display.FirstOrDefault() as Text;
                if (text != null)
                {
                    if (text.Italic)
                        inlineContainer.FontStyle = FontStyles.Italic;

                    if (text.Bold)
                        inlineContainer.FontWeight = FontWeights.Bold;


                    if (text.HeaderSize != 0)
                    {
                        switch (text.HeaderSize)
                        {
                            case 1:
                                inlineContainer.FontSize = 24;
                                break;
                            case 2:
                                inlineContainer.FontSize = 24;
                                inlineContainer.FontWeight = FontWeights.Bold;
                                inlineContainer.Foreground = _forgroundBrush;
                                break;
                            case 3:
                            case 4:
                            case 5:
                            case 6:
                                inlineContainer.FontSize = 28;
                                inlineContainer.FontWeight = FontWeights.Bold;
                                break;
                        }
                    }
                }
                else
                {
                    inlineContainer = new System.Windows.Documents.InlineUIContainer();
                    var fullUIVisitor = new SnuDomFullUIVisitor(_forgroundBrush);
                    //cant be null in this category
                    foreach (var item in link.Display)
                        item.Accept(fullUIVisitor);

                    ((InlineUIContainer)inlineContainer).Child = new RichMarkdownButton(link.Url, fullUIVisitor.Result);
                }
            }

            if (_currentParagraph == null)
            {
                MaybeSplitForParagraph();
            }

            _currentParagraph.Inlines.Add(inlineContainer);
        }

        public void Visit(Code code)
        {
            var plainTextVisitor = new SnuDomPlainTextVisitor();

            foreach (var item in code)
                item.Accept(plainTextVisitor);

            var madeRun = new Run { Text = plainTextVisitor.Result };
            if (_currentParagraph == null || code.IsBlock)
            {
                MaybeSplitForParagraph();
            }
            _currentParagraph.Inlines.Add(madeRun);
        }

        public void Visit(Quote code)
        {
            SnuDomCategoryVisitor categoryVisitor = new SnuDomCategoryVisitor();
            UIElement result = null;
            foreach (var item in code)
            {
                item.Accept(categoryVisitor);
            }


            if (categoryVisitor.Category == MarkdownCategory.PlainText && code.Count() == 1)
            {
                var plainTextVisitor = new SnuDomPlainTextVisitor();

                foreach (var item in code)
                    item.Accept(plainTextVisitor);


                result = new MarkdownQuote(plainTextVisitor.Result);
            }
            else
            {
                var fullUIVisitor = new SnuDomFullUIVisitor(_forgroundBrush);
                //cant be null in this category
                foreach (var item in code)
                    item.Accept(fullUIVisitor);

                result = new MarkdownQuote(fullUIVisitor.Result);
            }

            DirectlyPlaceUIContent(result);
        }

        private IEnumerable<UIElement> BuildChildUIList(IEnumerable<IDomObject> objects)
        {
            List<UIElement> results = new List<UIElement>();
            foreach (var item in objects)
            {
                SnuDomCategoryVisitor categoryVisitor = new SnuDomCategoryVisitor();


                if (item is TableColumn)
                {
                    foreach (var contents in ((TableColumn)item).Contents)
                    {
                        contents.Accept(categoryVisitor);
                    }
                }
                else
                {
                    item.Accept(categoryVisitor);
                }


                var column = item as TableColumn;
                IDomObject columnFirstContent = null;

                if (categoryVisitor.Category == MarkdownCategory.PlainText)
                {
                    var plainTextVisitor = new SnuDomPlainTextVisitor();
                    //this might be a pp
                    if (column != null)
                    {
                        foreach (var contents in column.Contents)
                        {
                            contents.Accept(plainTextVisitor);
                        }
                    }
                    else if(item is SnuDomWP8.Paragraph)
                    {
                        item.Accept(plainTextVisitor);
                    }

                    results.Add(new TextBlock { TextWrapping = System.Windows.TextWrapping.Wrap, Text = plainTextVisitor.Result, Margin = new Thickness(4, 6, 4, 6) });
                }
                else if (column != null && ((TableColumn)item).Contents.Count() == 1 && (columnFirstContent = ((TableColumn)item).Contents.FirstOrDefault()) != null &&
                    (columnFirstContent is Text))
                {
                    if (columnFirstContent is Link)
                    {
                        var plainTextVisitor = new SnuDomPlainTextVisitor();
                        var lnk = columnFirstContent as Link;
                        var firstContent = lnk.Display.FirstOrDefault();
                        if(firstContent != null)
                            firstContent.Accept(plainTextVisitor);
                        results.Add(new MarkdownButton(lnk.Url, plainTextVisitor.Result));
                    }
                    else
                    {
                        results.Add(new TextBlock { TextWrapping = System.Windows.TextWrapping.Wrap, Text = ((Text)columnFirstContent).Contents, Margin = new Thickness(4, 6, 4, 6) });
                    }
                }
                else
                {
                    var fullUIVisitor = new SnuDomFullUIVisitor(_forgroundBrush);
                    if (column != null)
                    {
                        foreach (var contents in column.Contents)
                        {
                            contents.Accept(fullUIVisitor);
                        }
                    }
                    else if (item is SnuDomWP8.Paragraph)
                    {
                        item.Accept(fullUIVisitor);
                    }

                    if (fullUIVisitor.ResultGroup != null)
                        results.Add(fullUIVisitor.ResultGroup);
                    else
                        results.Add(fullUIVisitor.Result);
                }

                if (column != null)
                {
                    switch (column.Alignment)
                    {
                        case ColumnAlignment.Center:
                            results.Last().SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                            break;
                        case ColumnAlignment.Left:
                            results.Last().SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
                            break;
                        case ColumnAlignment.Right:
                            results.Last().SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Right);
                            break;
                    }

                    results.Last().SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Top);
                }
            }
            return results;
        }

        public void Visit(OrderedList orderedList)
        {
            var uiElements = BuildChildUIList(orderedList);
            DirectlyPlaceUIContent(new MarkdownList(true, uiElements));
        }

        public void Visit(UnorderedList unorderedList)
        {
            var uiElements = BuildChildUIList(unorderedList);
            DirectlyPlaceUIContent(new MarkdownList(false, uiElements));
        }

        public void Visit(Table table)
        {
            var headerUIElements = BuildChildUIList(table.Headers);
            List<IEnumerable<UIElement>> tableBody = new List<IEnumerable<UIElement>>();
            foreach (var row in table.Rows)
            {
                tableBody.Add(BuildChildUIList(row.Columns));
            }

            DirectlyPlaceUIContent(new MarkdownTable(headerUIElements, tableBody));
        }

        public void Visit(Document document)
        {
            foreach (var elem in document)
            {
                elem.Accept(this);
            }
        }

        public void Visit(TableRow tableRow)
        {
            throw new NotImplementedException();
        }

        public void Visit(TableColumn tableColumn)
        {
            foreach (var elem in tableColumn.Contents)
            {
                elem.Accept(this);
            }
        }
    }
}
