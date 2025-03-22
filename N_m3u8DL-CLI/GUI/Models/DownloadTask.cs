using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace N_m3u8DL_CLI.GUI.Models
{
    /// <summary>
    /// 表示一个下载任务
    /// </summary>
    public class DownloadTask : INotifyPropertyChanged
    {
        public enum TaskStatus
        {
            Ready,      // 准备就绪
            Parsing,    // 解析中
            Downloading,// 下载中
            Paused,     // 已暂停
            Merging,    // 合并中
            Completed,  // 已完成
            Failed,     // 失败
            Deleted     // 已删除
        }

        private string _id;
        private string _url;
        private string _name;
        private string _savePath;
        private string _saveFileName;
        private TaskStatus _status;
        private double _progress;
        private long _totalSize;
        private long _downloadedSize;
        private double _speed;
        private string _errorMessage;
        private DateTime _createdTime;
        private DateTime _startTime;
        private DateTime _endTime;
        private StringBuilder _logBuilder;

        public DownloadTask()
        {
            Id = Guid.NewGuid().ToString();
            CreatedTime = DateTime.Now;
            Status = TaskStatus.Ready;
            Progress = 0;
            _logBuilder = new StringBuilder();
        }

        /// <summary>
        /// 任务唯一ID
        /// </summary>
        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 下载链接
        /// </summary>
        public string Url
        {
            get => _url;
            set
            {
                _url = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 任务名称
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 保存路径
        /// </summary>
        public string SavePath
        {
            get => _savePath;
            set
            {
                _savePath = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 保存文件名
        /// </summary>
        public string SaveFileName
        {
            get => _saveFileName;
            set
            {
                _saveFileName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 任务状态
        /// </summary>
        public TaskStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 下载进度 (0-100)
        /// </summary>
        public double Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 总文件大小 (字节)
        /// </summary>
        public long TotalSize
        {
            get => _totalSize;
            set
            {
                _totalSize = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormattedTotalSize));
            }
        }

        /// <summary>
        /// 已下载大小 (字节)
        /// </summary>
        public long DownloadedSize
        {
            get => _downloadedSize;
            set
            {
                _downloadedSize = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormattedDownloadedSize));
            }
        }

        /// <summary>
        /// 下载速度 (KB/s)
        /// </summary>
        public double Speed
        {
            get => _speed;
            set
            {
                _speed = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormattedSpeed));
            }
        }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime
        {
            get => _createdTime;
            set
            {
                _createdTime = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 任务开始时间
        /// </summary>
        public DateTime StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 任务结束时间
        /// </summary>
        public DateTime EndTime
        {
            get => _endTime;
            set
            {
                _endTime = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 格式化的总大小
        /// </summary>
        public string FormattedTotalSize => FormatSize(TotalSize);

        /// <summary>
        /// 格式化的已下载大小
        /// </summary>
        public string FormattedDownloadedSize => FormatSize(DownloadedSize);

        /// <summary>
        /// 格式化的下载速度
        /// </summary>
        public string FormattedSpeed
        {
            get
            {
                if (Speed <= 0)
                    return "0 KB/s";
                else if (Speed < 1024)
                    return $"{Speed:F1} KB/s";
                else
                    return $"{Speed / 1024:F1} MB/s";
            }
        }

        /// <summary>
        /// 获取任务日志
        /// </summary>
        public string Log => _logBuilder.ToString();

        /// <summary>
        /// 添加日志
        /// </summary>
        public void AddToLog(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                _logBuilder.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
                OnPropertyChanged(nameof(Log));
            }
        }

        /// <summary>
        /// 将字节大小格式化为可读的字符串
        /// </summary>
        private string FormatSize(long bytes)
        {
            if (bytes < 0)
                return "未知";

            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int unitIndex = 0;

            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return $"{size:F2} {units[unitIndex]}";
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
