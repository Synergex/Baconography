using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace BaconographyWP8.Converters
{
    class TimeRelationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var currentTime = DateTime.UtcNow;
            var timeDifference = DateTimeSpan.CompareDates(currentTime, (DateTime)value);
            if (timeDifference.Years > 0)
                return string.Format("{0} Year{1} ago", timeDifference.Years, timeDifference.Years > 1 ? "s" : "");
            else if (timeDifference.Months > 0)
                return string.Format("{0} Month{1} ago", timeDifference.Months, timeDifference.Months > 1 ? "s" : "");
            else if (timeDifference.Days > 0)
                return string.Format("{0} Day{1} ago", timeDifference.Days, timeDifference.Days > 1 ? "s" : "");
            else if (timeDifference.Hours > 0)
                return string.Format("{0} Hour{1} ago", timeDifference.Hours, timeDifference.Hours > 1 ? "s" : "");
            else if (timeDifference.Minutes > 0)
                return string.Format("{0} Minute{1} ago", timeDifference.Minutes, timeDifference.Minutes > 1 ? "s" : "");
            else
                return "Just Now";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// from Kirk Woll http://stackoverflow.com/questions/4638993/difference-in-months-between-two-dates
        /// </summary>
        public struct DateTimeSpan
        {
            private readonly int years;
            private readonly int months;
            private readonly int days;
            private readonly int hours;
            private readonly int minutes;
            private readonly int seconds;
            private readonly int milliseconds;

            public DateTimeSpan(int years, int months, int days, int hours, int minutes, int seconds, int milliseconds)
            {
                this.years = years;
                this.months = months;
                this.days = days;
                this.hours = hours;
                this.minutes = minutes;
                this.seconds = seconds;
                this.milliseconds = milliseconds;
            }

            public int Years { get { return years; } }
            public int Months { get { return months; } }
            public int Days { get { return days; } }
            public int Hours { get { return hours; } }
            public int Minutes { get { return minutes; } }
            public int Seconds { get { return seconds; } }
            public int Milliseconds { get { return milliseconds; } }

            enum Phase { Years, Months, Days, Done }

            public static DateTimeSpan CompareDates(DateTime date1, DateTime date2)
            {

                if (date2 < date1)
                {
                    var sub = date1;
                    date1 = date2;
                    date2 = sub;
                }

                DateTime current = date2;
                int years = 0;
                int months = 0;
                int days = 0;

                Phase phase = Phase.Years;
                DateTimeSpan span = new DateTimeSpan();

                while (phase != Phase.Done)
                {
                    switch (phase)
                    {
                        case Phase.Years:
                            if (current.Year == 1 || current.AddYears(-1) < date1)
                            {
                                phase = Phase.Months;
                            }
                            else
                            {
                                current = current.AddYears(-1);
                                years++;
                            }
                            break;
                        case Phase.Months:
                            if (current.AddMonths(-1) < date1)
                            {
                                phase = Phase.Days;
                            }
                            else
                            {
                                current = current.AddMonths(-1);
                                months++;
                            }
                            break;
                        case Phase.Days:
                            if (current.AddDays(-1) < date1)
                            {
                                var timespan = current - date1;
                                span = new DateTimeSpan(years, months, days, timespan.Hours, timespan.Minutes, timespan.Seconds, timespan.Milliseconds);
                                phase = Phase.Done;
                            }
                            else
                            {
                                current = current.AddDays(-1);
                                days++;
                            }
                            break;
                    }
                }

                return span;

            }
        }
    }
}
