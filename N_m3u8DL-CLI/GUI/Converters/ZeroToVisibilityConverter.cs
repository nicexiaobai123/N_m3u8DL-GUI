using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace N_m3u8DL_CLI.GUI
{
    /// <summary>
    /// 将数值转换为可见性的转换器
    /// 当值为0时返回Collapsed，否则返回Visible
    /// </summary>
    public class ZeroToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 如果值为空，返回Collapsed
            if (value == null)
                return Visibility.Collapsed;

            // 尝试将值转换为整数
            if (int.TryParse(value.ToString(), out int count))
            {
                // 如果值为0，返回Collapsed，否则返回Visible
                return count == 0 ? Visibility.Collapsed : Visibility.Visible;
            }

            // 默认返回Visible
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 这个转换器不支持双向绑定
            throw new NotImplementedException();
        }
    }
}
