namespace MediaCat.Converters {
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    /// <summary>Converts boolean values to string values. The values for both boolean states can be set via dependency properties. Does not support reverse conversion (string -> boolean)</summary>
    [ValueConversion(typeof(bool), typeof(string))]
    public sealed class BooleanToStringConverter : DependencyObject, IValueConverter {

        /// <summary>Property specifying the string to return when the parameter is true.</summary>
        public static readonly DependencyProperty TrueTextProperty =
            DependencyProperty.Register(nameof(TrueText), typeof(string), typeof(BooleanToStringConverter), new PropertyMetadata("Yes"));

        /// <summary>Property specifying the string to return when the parameter is false.</summary>
        public static readonly DependencyProperty FalseTextProperty =
            DependencyProperty.Register(nameof(FalseText), typeof(string), typeof(BooleanToStringConverter), new PropertyMetadata("No"));

        /// <summary>The text value to use if the boolean is false.</summary>
        public string FalseText {
            get => (string) GetValue(FalseTextProperty);
            set => SetValue(FalseTextProperty, value);
        }

        /// <summary>The text value to use if the boolean is true.</summary>
        public string TrueText {
            get => (string) GetValue(TrueTextProperty);
            set => SetValue(TrueTextProperty, value);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            bool result = (value is bool val) ? val : (value != null);

            return result ? this.TrueText : this.FalseText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }

    }

}