using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AIT.DMF.DependencyManager.Controls.Converters
{
    public class NullToVisibilityConverter : IValueConverter
    {
        #region Implementation of IValueConverter

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var invert = false;
            if (parameter != null)
            {
                bool.TryParse(parameter.ToString(), out invert);
            }

            if (invert)
            {
                return value == null ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                return value == null ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
