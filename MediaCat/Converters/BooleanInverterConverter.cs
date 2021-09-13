namespace MediaCat.Converters {
    using System;
    using System.Globalization;
    using System.Windows.Data;

    /// <summary>Converts boolean values by inverting them (a -> !a).</summary>
    [ValueConversion(typeof(bool), typeof(bool))]
    public sealed class BooleanInverterConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is bool b)
                return !b;
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return Convert(value, targetType, parameter, culture);
        }

    }

}