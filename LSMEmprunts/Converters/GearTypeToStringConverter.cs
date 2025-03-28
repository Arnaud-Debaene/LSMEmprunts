using LSMEmprunts.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace LSMEmprunts
{
    public class GearTypeToStringConverter : IValueConverter
    {
        private static readonly Dictionary<GearType, string> _Labels = new Dictionary<GearType, string>
        {
            { GearType.BCD, "Stab" },
            { GearType.Regulator, "Détendeur" },
            { GearType.Tank, "Bloc" }
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var enumerable = value as IEnumerable<GearType>;
            if (enumerable != null)
            {
                return new List<string>(enumerable.Select(e => _Labels[e]));                
            }
            else
            {
                var type = (GearType)value;
                switch (type)
                {
                    case GearType.BCD:
                        return "Stab";
                    case GearType.Regulator:
                        return "Détendeur";
                    case GearType.Tank:
                        return "Bloc";
                }
            }
            throw new ArgumentOutOfRangeException(nameof(value));

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var s = (string)value;
            return _Labels.First(e => e.Value == s).Key;
        }
    }
}
