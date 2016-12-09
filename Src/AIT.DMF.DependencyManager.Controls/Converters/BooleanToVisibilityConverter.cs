// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BooleanToVisibilityConverter.cs" company="AIT GmbH & Co. KG">
//   All rights reserved by AIT GmbH & Co. KG
// </copyright>
// <summary>
//   Defines the BooleanToVisibilityConverter type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace AIT.DMF.DependencyManager.Controls.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    /// <summary>
    /// Boolean to visibility converter.
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        #region Implementation of IValueConverter

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var invert = false;
            if (parameter != null)
            {
                bool.TryParse(parameter.ToString(), out invert);
            }

            if (!invert)
            {
                return (bool)value ? Visibility.Visible : Visibility.Collapsed;
            }

            return (bool)value ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
