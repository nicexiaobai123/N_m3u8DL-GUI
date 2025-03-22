using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace N_m3u8DL_CLI.GUI
{
    /// <summary>
    /// 将字符串转换为可见性，如果字符串为空则隐藏
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                return string.IsNullOrWhiteSpace(stringValue) ? Visibility.Collapsed : Visibility.Visible;
            }
            
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
