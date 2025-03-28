using System;
using System.Globalization;
using System.Windows.Data;
using LSMEmprunts.Data;

namespace LSMEmprunts
{
    class BorrowStateToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var state = (BorrowingState) value;
            switch (state)
            {
                case BorrowingState.Open:
                    return "En cours";
                case BorrowingState.GearReturned:
                    return "Clos";
                case BorrowingState.ForcedClose:
                    return "Clos de force";
            }
            throw new ArgumentOutOfRangeException(nameof(value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
