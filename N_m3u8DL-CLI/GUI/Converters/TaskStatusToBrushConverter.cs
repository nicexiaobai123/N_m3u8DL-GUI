using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using N_m3u8DL_CLI.GUI.Models;

namespace N_m3u8DL_CLI.GUI
{
    /// <summary>
    /// 将任务状态转换为相应的颜色画刷
    /// </summary>
    public class TaskStatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DownloadTask.TaskStatus status)
            {
                switch (status)
                {
                    case DownloadTask.TaskStatus.Ready:
                        return new SolidColorBrush(Colors.Gray);
                    case DownloadTask.TaskStatus.Parsing:
                        return new SolidColorBrush(Colors.Blue);
                    case DownloadTask.TaskStatus.Downloading:
                        return new SolidColorBrush((Color)App.Current.Resources["PrimaryColor"]);
                    case DownloadTask.TaskStatus.Paused:
                        return new SolidColorBrush(Colors.Orange);
                    case DownloadTask.TaskStatus.Completed:
                        return new SolidColorBrush((Color)App.Current.Resources["SuccessColor"]);
                    case DownloadTask.TaskStatus.Failed:
                        return new SolidColorBrush((Color)App.Current.Resources["ErrorColor"]);
                    case DownloadTask.TaskStatus.Deleted:
                        return new SolidColorBrush(Colors.Gray);
                    default:
                        return new SolidColorBrush(Colors.Gray);
                }
            }
            
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
