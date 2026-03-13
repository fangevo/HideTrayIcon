using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TrayZen.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object parameter, CultureInfo c) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type t, object parameter, CultureInfo c) =>
        value is Visibility.Visible;
}

public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type t, object parameter, CultureInfo c) =>
        value is bool b ? !b : value;

    public object ConvertBack(object value, Type t, object parameter, CultureInfo c) =>
        value is bool b ? !b : value;
}

public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type t, object parameter, CultureInfo c) =>
        value != null ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type t, object parameter, CultureInfo c) =>
        throw new NotSupportedException();
}

public sealed class BoolToTextConverter : IValueConverter
{
    public string TrueText { get; set; } = "Yes";
    public string FalseText { get; set; } = "No";

    public object Convert(object value, Type t, object parameter, CultureInfo c) =>
        value is true ? TrueText : FalseText;

    public object ConvertBack(object value, Type t, object parameter, CultureInfo c) =>
        throw new NotSupportedException();
}
