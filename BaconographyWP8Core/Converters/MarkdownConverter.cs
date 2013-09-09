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
            return new TextBlock { Text = value as string, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(10, 0, 0, 0) };
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
        public RichTextBox Result = new RichTextBox { TextWrapping = TextWrapping.Wrap };
        System.Windows.Documents.Paragraph _currentParagraph;
        public void Visit(Text text)
        {
            var madeRun = new Run { Text = text.Contents };


            if (text.Italic)
                madeRun.FontStyle = FontStyles.Italic;

            if (text.Bold)
                madeRun.FontWeight = FontWeights.Bold;


            if (text.HeaderSize != 0)
            {
                switch (text.HeaderSize)
                {
                    case 1:
                        madeRun.FontSize = 12;
                        break;
                    case 2:
                        madeRun.FontSize = 16;
                        break;
                    case 3:
                        madeRun.FontSize = 20;
                        break;
                    case 4:
                        madeRun.FontSize = 24;
                        break;
                    case 5:
                        madeRun.FontSize = 28;
                        break;
                    case 6:
                        madeRun.FontSize = 32;
                        break;
                }
                _currentParagraph = new System.Windows.Documents.Paragraph();
                Result.Blocks.Add(_currentParagraph);
                _currentParagraph.Inlines.Add(madeRun);
                _currentParagraph.Inlines.Add(new System.Windows.Documents.LineBreak());
            }
            else
            {
                if (_currentParagraph == null)
                {
                    _currentParagraph = new System.Windows.Documents.Paragraph();
                    Result.Blocks.Add(_currentParagraph);
                }
                _currentParagraph.Inlines.Add(madeRun);
            }
        }

        public void Visit(SnuDomWP8.Paragraph paragraph)
        {
            _currentParagraph = new System.Windows.Documents.Paragraph();
            Result.Blocks.Add(_currentParagraph);
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
            _currentParagraph = new System.Windows.Documents.Paragraph();
            _currentParagraph.Inlines.Add(inlineContainer);
            Result.Blocks.Add(_currentParagraph);
        }

        public void Visit(SnuDomWP8.LineBreak lineBreak)
        {
            _currentParagraph.Inlines.Add(new System.Windows.Documents.LineBreak());
        }

        public void Visit(Link link)
        {
            var inlineContainer = new System.Windows.Documents.InlineUIContainer();

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

                inlineContainer.Child = new MarkdownButton(link.Url, plainTextVisitor.Result);
            }
            else
            {
                var fullUIVisitor = new SnuDomFullUIVisitor(_forgroundBrush);
                //cant be null in this category
                foreach (var item in link.Display)
                    item.Accept(fullUIVisitor);

                inlineContainer.Child = new RichMarkdownButton(link.Url, fullUIVisitor.Result);
            }
            
            _currentParagraph.Inlines.Add(inlineContainer);
        }

        public void Visit(Code code)
        {
            var plainTextVisitor = new SnuDomPlainTextVisitor();

            foreach (var item in code)
                item.Accept(plainTextVisitor);

            var madeRun = new Run { Text = plainTextVisitor.Result };
            if (_currentParagraph == null)
            {
                _currentParagraph = new System.Windows.Documents.Paragraph();
                Result.Blocks.Add(_currentParagraph);
            }
            _currentParagraph.Inlines.Add(madeRun);
        }

        public void Visit(Quote code)
        {
            var inlineContainer = new InlineUIContainer();

            SnuDomCategoryVisitor categoryVisitor = new SnuDomCategoryVisitor();

            foreach (var item in code)
            {
                item.Accept(categoryVisitor);
            }


            if (categoryVisitor.Category == MarkdownCategory.PlainText)
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
                _currentParagraph = new System.Windows.Documents.Paragraph();
                Result.Blocks.Add(_currentParagraph);
            }
            else
            {
                _currentParagraph.Inlines.Add(new System.Windows.Documents.LineBreak());
            }

            _currentParagraph.Inlines.Add(inlineContainer);
            _currentParagraph.Inlines.Add(new System.Windows.Documents.LineBreak());
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
                    var column = item as TableColumn;
                    foreach (var contents in column.Contents)
                    {
                        contents.Accept(plainTextVisitor);
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
            _currentParagraph = new System.Windows.Documents.Paragraph();
            _currentParagraph.Inlines.Add(inlineContainer);
            Result.Blocks.Add(_currentParagraph);
        }

        public void Visit(UnorderedList unorderedList)
        {
            var uiElements = BuildChildUIList(unorderedList);
            var inlineContainer = new InlineUIContainer();
            inlineContainer.Child = new MarkdownList(false, uiElements);
            _currentParagraph = new System.Windows.Documents.Paragraph();
            _currentParagraph.Inlines.Add(inlineContainer);
            Result.Blocks.Add(_currentParagraph);
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
                _currentParagraph = new System.Windows.Documents.Paragraph();
                Result.Blocks.Add(_currentParagraph);
            }
            else
            {
                _currentParagraph.Inlines.Add(new System.Windows.Documents.LineBreak());
            }

            _currentParagraph.Inlines.Add(inlineContainer);
            _currentParagraph.Inlines.Add(new System.Windows.Documents.LineBreak());
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
