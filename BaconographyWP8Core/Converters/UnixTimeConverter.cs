using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace BaconographyWP8.Converters
{
    public class UnixTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var dateTime = (DateTime)value;
            var currentTime = DateTime.UtcNow;

            if (dateTime.Day == currentTime.Day)
            {
                var hour = dateTime.TimeOfDay.Hours;
                var ampm = hour > 12 ? "pm" : "am";
                hour = hour > 12 ? hour - 12 : hour;
                string minute = dateTime.TimeOfDay.Minutes < 10 ? ("0" + dateTime.TimeOfDay.Minutes) : dateTime.TimeOfDay.Minutes.ToString();
                return hour + ":" + minute + " " + ampm;
            }
            else
            {
                if (dateTime.Year == currentTime.Year)
                    return AbbreviatedMonth(dateTime.Month) + " " + dateTime.Day;
                else
                    return dateTime.Year.ToString();
            }
        }

        public string AbbreviatedMonth(int month)
        {
            switch (month)
            {
                case 1:
                    return "Jan";
                case 2:
                    return "Feb";
                case 3:
                    return "Mar";
                case 4:
                    return "Apr";
                case 5:
                    return "May";
                case 6:
                    return "Jun";
                case 7:
                    return "Jul";
                case 8:
                    return "Aug";
                case 9:
                    return "Sep";
                case 10:
                    return "Oct";
                case 11:
                    return "Nov";
                case 12:
                    return "Dec";

                default:
                    return "Jan";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
