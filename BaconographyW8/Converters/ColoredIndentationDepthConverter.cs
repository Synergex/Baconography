using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;

namespace BaconographyW8.Converters
{
    public class ColoredIndentationDepthConverter : IValueConverter
    {
        static SolidColorBrush even = new SolidColorBrush(Color.FromArgb(255, 80, 80, 80));
        static SolidColorBrush odd = new SolidColorBrush(Color.FromArgb(255, 50, 50, 50));


        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int intVal = (int)value;
            var grid = new Grid { VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch, Width = intVal * 24, HorizontalAlignment = HorizontalAlignment.Left};
            
            for (int i = 0; i < intVal; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24.0) });
                var rect = new Rectangle { Fill = i % 2 == 0 ? even : odd, Width = 24, VerticalAlignment = VerticalAlignment.Stretch };
                Grid.SetColumn(rect, i);
                grid.Children.Add(rect);
            }
            return grid;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
