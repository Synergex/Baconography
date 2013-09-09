using BaconographyPortable.Services;
using BaconographyPortable.ViewModel;
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
using SnuDom;
using Windows.UI.Xaml.Media;
using Windows.UI.Text;
using BaconographyW8.View.Markdown;
using Windows.UI;

namespace BaconographyW8.Converters
{
    public class MarkdownConverter : IValueConverter
    {
        unsafe public object Convert(object value, Type targetType, object parameter, string language)
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
                                var visitor = new SnuDomFullUIVisitor(new SolidColorBrush(Colors.White));
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
                catch (Exception ex)
                {
                    return MakePlain(ex.ToString());
                }
            }
            else
                return new TextBlock { Text = "" };
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

    class SnuDomFullUIVisitor : IDomVisitor
    {
        public SnuDomFullUIVisitor(Brush forgroundBrush)
        {
            _forgroundBrush = forgroundBrush;
        }
        Brush _forgroundBrush;
        private int _textLengthInCurrent = 0;
        public RichTextBlock Result = new RichTextBlock { TextWrapping = TextWrapping.Wrap };
        public StackPanel ResultGroup = null;
        Windows.UI.Xaml.Documents.Paragraph _currentParagraph;

        private void MaybeSplitForParagraph()
        {
            if (_textLengthInCurrent > 1000)
            {
                if (ResultGroup == null)
                {
                    ResultGroup = new StackPanel { Orientation = Orientation.Vertical };
                    ResultGroup.Children.Add(Result);
                }

                ResultGroup.Children.Add(Result = new RichTextBlock { TextWrapping = TextWrapping.Wrap });
                _textLengthInCurrent = 0;
            }

            _currentParagraph = new Windows.UI.Xaml.Documents.Paragraph();
            Result.Blocks.Add(_currentParagraph);
        }

        public void Visit(Text text)
        {
            var madeRun = new Run { Text = text.Contents };
            _textLengthInCurrent += text.Contents.Length;

            if (text.Italic)
                madeRun.FontStyle = Windows.UI.Text.FontStyle.Italic;

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
                    var inlineContainer = new InlineUIContainer();
                    inlineContainer.Child = new Border
                    {
                        Margin = new Thickness(0, 5, 0, 5),
                        Height = 1,
                        VerticalAlignment = VerticalAlignment.Top,
                        BorderBrush = _forgroundBrush,
                        BorderThickness = new Thickness(1),
                        MinWidth = 1800
                    };
                    _currentParagraph.Inlines.Add(inlineContainer);
                }
                else
                    _currentParagraph.Inlines.Add(new Windows.UI.Xaml.Documents.LineBreak());

            }
            else
            {
                if (_currentParagraph == null)
                {
                    _currentParagraph = new Windows.UI.Xaml.Documents.Paragraph();
                    Result.Blocks.Add(_currentParagraph);
                }
                _currentParagraph.Inlines.Add(madeRun);
            }
        }

        public void Visit(SnuDom.Paragraph paragraph)
        {
            MaybeSplitForParagraph();
            foreach (var elem in paragraph)
            {
                elem.Accept(this);
            }

        }

        public void Visit(HorizontalRule horizontalRule)
        {
            var inlineContainer = new InlineUIContainer();
            inlineContainer.Child = new Border
            {
                Margin = new Thickness(0, 5, 0, 5),
                Height = 2,
                VerticalAlignment = VerticalAlignment.Top,
                BorderBrush = _forgroundBrush,
                BorderThickness = new Thickness(2),
                MinWidth = 1800
            };
            MaybeSplitForParagraph();
            _currentParagraph.Inlines.Add(inlineContainer);
        }

        public void Visit(SnuDom.LineBreak lineBreak)
        {
            _currentParagraph.Inlines.Add(new Windows.UI.Xaml.Documents.LineBreak());
        }

        public void Visit(Link link)
        {
            var inlineContainer = new InlineUIContainer();

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
                if (link.Display != null)
                {
                    foreach (var item in link.Display)
                        item.Accept(plainTextVisitor);
                }
                else
                    plainTextVisitor.Result = link.Url;

                inlineContainer.Child = new MarkdownLink(link.Url, plainTextVisitor.Result);
            }
            else
            {
                var fullUIVisitor = new SnuDomFullUIVisitor(_forgroundBrush);
                //cant be null in this category
                foreach (var item in link.Display)
                    item.Accept(fullUIVisitor);

                inlineContainer.Child = new MarkdownLink(link.Url, fullUIVisitor.Result);
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

            if (code.IsBlock)
            {
                _currentParagraph.Inlines.Add(new Windows.UI.Xaml.Documents.LineBreak());
            }
        }

        public void Visit(Quote code)
        {
            var inlineContainer = new InlineUIContainer();

            SnuDomCategoryVisitor categoryVisitor = new SnuDomCategoryVisitor();

            foreach (var item in code)
            {
                item.Accept(categoryVisitor);
            }


            if (categoryVisitor.Category == MarkdownCategory.PlainText && code.Count() == 1)
            {
                var plainTextVisitor = new SnuDomPlainTextVisitor();

                foreach (var item in code)
                    item.Accept(plainTextVisitor);


                inlineContainer.Child = new MarkdownQuote(plainTextVisitor.Result);
            }
            else
            {
                var fullUIVisitor = new SnuDomFullUIVisitor(_forgroundBrush);
                //cant be null in this category
                foreach (var item in code)
                    item.Accept(fullUIVisitor);

                inlineContainer.Child = new MarkdownQuote(fullUIVisitor.Result);
            }

            if (_currentParagraph == null)
            {
                MaybeSplitForParagraph();
            }
            else
            {
                _currentParagraph.Inlines.Add(new Windows.UI.Xaml.Documents.LineBreak());
            }

            _currentParagraph.Inlines.Add(inlineContainer);
            _currentParagraph.Inlines.Add(new Windows.UI.Xaml.Documents.LineBreak());
        }

        private IEnumerable<UIElement> BuildChildUIList(IEnumerable<IDomObject> objects)
        {
            List<UIElement> results = new List<UIElement>();
            foreach (var item in objects)
            {
                SnuDomCategoryVisitor categoryVisitor = new SnuDomCategoryVisitor();
                item.Accept(categoryVisitor);
                if (categoryVisitor.Category == MarkdownCategory.PlainText)
                {
                    var plainTextVisitor = new SnuDomPlainTextVisitor();
                    //this might be a pp
                    var column = item as TableColumn;
                    if (column != null)
                    {
                        foreach (var contents in column.Contents)
                        {
                            contents.Accept(plainTextVisitor);
                        }
                    }
                    else if (item is SnuDom.Paragraph)
                    {
                        item.Accept(plainTextVisitor);
                    }

                    results.Add(new TextBlock { Text = plainTextVisitor.Result });
                }
                else
                {
                    var fullUIVisitor = new SnuDomFullUIVisitor(_forgroundBrush);
                    item.Accept(fullUIVisitor);
                    results.Add(fullUIVisitor.Result);
                }
            }
            return results;
        }

        public void Visit(OrderedList orderedList)
        {
            var uiElements = BuildChildUIList(orderedList);
            var inlineContainer = new InlineUIContainer();
            inlineContainer.Child = new MarkdownList(true, uiElements);
            MaybeSplitForParagraph();
            _currentParagraph.Inlines.Add(inlineContainer);
        }

        public void Visit(UnorderedList unorderedList)
        {
            var uiElements = BuildChildUIList(unorderedList);
            var inlineContainer = new InlineUIContainer();
            inlineContainer.Child = new MarkdownList(false, uiElements);
            MaybeSplitForParagraph();
            _currentParagraph.Inlines.Add(inlineContainer);
        }

        public void Visit(Table table)
        {
            var headerUIElements = BuildChildUIList(table.Headers);
            List<IEnumerable<UIElement>> tableBody = new List<IEnumerable<UIElement>>();
            foreach (var row in table.Rows)
            {
                tableBody.Add(BuildChildUIList(row.Columns));
            }
            var inlineContainer = new InlineUIContainer();
            inlineContainer.Child = new MarkdownTable(headerUIElements, tableBody);

            if (_currentParagraph == null)
            {
                MaybeSplitForParagraph();
            }
            else
            {
                _currentParagraph.Inlines.Add(new Windows.UI.Xaml.Documents.LineBreak());
            }

            _currentParagraph.Inlines.Add(inlineContainer);
            _currentParagraph.Inlines.Add(new Windows.UI.Xaml.Documents.LineBreak());
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
