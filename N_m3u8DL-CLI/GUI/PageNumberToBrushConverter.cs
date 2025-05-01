using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace N_m3u8DL_CLI.GUI
{
    /// <summary>
    /// 将页码是否为当前页转换为背景颜色
    /// </summary>
    public class PageNumberToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isCurrentPage && isCurrentPage)
            {
                // 当前页使用高亮颜色
                return new SolidColorBrush(Color.FromRgb(255, 87, 153));
            }
            
            // 非当前页使用普通背景色
            return new SolidColorBrush(Color.FromRgb(240, 240, 245));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 将页码是否为当前页转换为前景色
    /// </summary>
    public class PageNumberToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isCurrentPage && isCurrentPage)
            {
                // 当前页使用白色文本
                return new SolidColorBrush(Colors.White);
            }
            
            // 非当前页使用深色文本
            return new SolidColorBrush(Color.FromRgb(60, 60, 60));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
