using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ProcessExplorerPro.Helpers
{
    // Converter to toggle side panel visibility depending on whether a process is selected
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isNull = value == null;
            if (parameter != null && parameter.ToString() == "Inverse")
            {
                return isNull ? Visibility.Visible : Visibility.Collapsed;
            }
            return isNull ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Converter for Radio Button binding to Enum/String properties
    public class ComparisonConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;
            return value.ToString() == parameter.ToString();
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || parameter == null || !(bool)value) return Binding.DoNothing;
            return parameter.ToString()!;
        }
    }

    // Converter to highlight high CPU usage numbers (e.g. values > 5.0%)
    public class HighUsageConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double val && parameter != null && double.TryParse(parameter.ToString(), out double limit))
            {
                return val > limit;
            }
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
