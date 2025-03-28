using LSMEmprunts.Data;
using System;
using System.Globalization;
using System.Windows.Data;

namespace LSMEmprunts
{
    public class GearDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var gear = (Gear)value;
            return string.IsNullOrEmpty(gear.Size) ? gear.Name : $"{gear.Name} ({gear.Size})";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}