using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DotNetBay.WPF
{
    public class BooleanToStatusTextConverter : IValueConverter
    {
        private const string OpenString = "Valid";

        private const string ClosedString = "Closed";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                return (bool)value ? OpenString : ClosedString;
            }

            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var val = value as String;

            if (val != null)
            {
                switch (val)
                {
                    case OpenString:
                        return true;
                    case ClosedString:
                        return false;
                }
            }

            throw new NotImplementedException();
        }
    }
}