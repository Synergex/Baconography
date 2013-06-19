using BaconographyPortable.Model.Reddit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace BaconographyWP8.Converters
{
    public class VoteIndicatorConverter : IValueConverter
    {
        private static Brush OrangeRed = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x45, 0x00));
        private static Brush LightSkyBlue = new SolidColorBrush(Color.FromArgb(0xFF, 0x87, 0xCE, 0xFA));
        private static FontFamily SegoeUISymbol = new FontFamily("Segoe UI Symbol");
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var votable = value as IVotable;
            if (votable != null && votable.Likes != null)
            {
                if (votable.Likes.Value)
                {
                    return new TextBlock
                    {
                        Foreground = OrangeRed,
                        FontSize = 13,
                        Margin = new System.Windows.Thickness(0),
                        FontFamily = SegoeUISymbol,
                        Text = "\uE110"
                    };
                }
                else
                {
                    var newTextBlock = new TextBlock
                    {
                        Foreground = LightSkyBlue,
                        FontSize = 13,
                        Margin = new System.Windows.Thickness(0),
                        FontFamily = SegoeUISymbol,
                        Text = "\uE110"
                    };

                    newTextBlock.RenderTransform = new RotateTransform { Angle = 180, CenterX = 9, CenterY = 9 };
                    return newTextBlock;
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
