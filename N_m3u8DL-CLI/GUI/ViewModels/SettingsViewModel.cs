using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using N_m3u8DL_CLI.GUI.Helpers;
using N_m3u8DL_CLI.GUI.Services;
using System.Windows.Forms;

namespace N_m3u8DL_CLI.GUI.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private string _defaultSavePath;
        private int _maxThreads = 32;
        private int _minThreads = 16;
        private int _retryCount = 15;
        private int _timeOut = 10;
        private bool _enableMuxFastStart = true;
        private bool _enableBinaryMerge = false;
        private bool _enableDelAfterDone = false;
        private bool _disableDateInfo = false;
        private bool _noProxy = false;
        private string _proxyAddress = "";
        private long _maxSpeed = 0;

        public SettingsViewModel()
        {
            // 初始化命令
            BrowseFolderCommand = new RelayCommand(BrowseFolder);
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
            ResetCommand = new RelayCommand(Reset);
            
            // 加载其他设置
            LoadSettings();
            
            // 设置默认保存路径为应用程序目录下的Download文件夹，如果需要
            string downloadPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Download");
                
            // 确保目录存在
            if (!Directory.Exists(downloadPath))
            {
                Directory.CreateDirectory(downloadPath);
            }
            
            // 只有当DefaultSavePath为空或者不存在时，才设置默认保存路径
            if (string.IsNullOrWhiteSpace(DefaultSavePath) || !Directory.Exists(DefaultSavePath))
            {
                DefaultSavePath = downloadPath;
            }
        }

        /// <summary>
        /// 默认保存路径
        /// </summary>
        public string DefaultSavePath
        {
            get => _defaultSavePath;
            set
            {
                _defaultSavePath = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 最大线程数
        /// </summary>
        public int MaxThreads
        {
            get => _maxThreads;
            set
            {
                _maxThreads = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 最小线程数
        /// </summary>
        public int MinThreads
        {
            get => _minThreads;
            set
            {
                _minThreads = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount
        {
            get => _retryCount;
            set
            {
                _retryCount = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 超时时间 (秒)
        /// </summary>
        public int TimeOut
        {
            get => _timeOut;
            set
            {
                _timeOut = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 启用MP4 FastStart特性
        /// </summary>
        public bool EnableMuxFastStart
        {
            get => _enableMuxFastStart;
            set
            {
                _enableMuxFastStart = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 启用二进制合并
        /// </summary>
        public bool EnableBinaryMerge
        {
            get => _enableBinaryMerge;
            set
            {
                _enableBinaryMerge = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 下载完成后删除临时文件
        /// </summary>
        public bool EnableDelAfterDone
        {
            get => _enableDelAfterDone;
            set
            {
                _enableDelAfterDone = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 关闭混流中的日期写入
        /// </summary>
        public bool DisableDateInfo
        {
            get => _disableDateInfo;
            set
            {
                _disableDateInfo = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 不使用系统代理
        /// </summary>
        public bool NoProxy
        {
            get => _noProxy;
            set
            {
                _noProxy = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 代理地址
        /// </summary>
        public string ProxyAddress
        {
            get => _proxyAddress;
            set
            {
                _proxyAddress = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 下载速度上限 (KB/s)
        /// </summary>
        public long MaxSpeed
        {
            get => _maxSpeed;
            set
            {
                _maxSpeed = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 浏览文件夹命令
        /// </summary>
        public ICommand BrowseFolderCommand { get; }

        /// <summary>
        /// 保存设置命令
        /// </summary>
        public ICommand SaveCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// 重置设置命令
        /// </summary>
        public ICommand ResetCommand { get; }

        /// <summary>
        /// 加载设置
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                var settings = SettingsManager.Instance.Settings;
                
                DefaultSavePath = settings.DefaultSavePath;
                
                MaxThreads = settings.MaxThreads;
                MinThreads = settings.MinThreads;
                RetryCount = settings.RetryCount;
                TimeOut = settings.TimeOut;
                MaxSpeed = settings.MaxSpeed;
                NoProxy = settings.NoProxy;
                ProxyAddress = settings.ProxyAddress;
                EnableBinaryMerge = settings.EnableBinaryMerge;
                EnableDelAfterDone = settings.EnableDelAfterDone;
                EnableMuxFastStart = settings.EnableMuxFastStart;
                DisableDateInfo = settings.DisableDateInfo;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"加载设置时出错：{ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        private void Save(object parameter)
        {
            try
            {
                var settings = SettingsManager.Instance.Settings;
                
                settings.DefaultSavePath = DefaultSavePath;
                settings.MaxThreads = MaxThreads;
                settings.MinThreads = MinThreads;
                settings.RetryCount = RetryCount;
                settings.TimeOut = TimeOut;
                settings.MaxSpeed = (int)MaxSpeed;
                settings.NoProxy = NoProxy;
                settings.ProxyAddress = ProxyAddress;
                settings.EnableBinaryMerge = EnableBinaryMerge;
                settings.EnableDelAfterDone = EnableDelAfterDone;
                settings.EnableMuxFastStart = EnableMuxFastStart;
                settings.DisableDateInfo = DisableDateInfo;
                
                // 保存设置
                SettingsManager.Instance.SaveSettings();
                
                // 关闭窗口
                if (parameter is Window window)
                {
                    window.DialogResult = true;
                    window.Close();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"保存设置时出错：{ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 取消设置
        /// </summary>
        private void Cancel(object parameter)
        {
            if (parameter is Window window)
            {
                window.DialogResult = false;
                window.Close();
            }
        }

        /// <summary>
        /// 重置设置为默认值
        /// </summary>
        private void Reset(object parameter)
        {
            try
            {
                var result = System.Windows.MessageBox.Show("确定要将所有设置重置为默认值吗？", "确认", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    DefaultSavePath = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "Download");
                    MaxThreads = 32;
                    MinThreads = 16;
                    RetryCount = 15;
                    TimeOut = 10;
                    MaxSpeed = 0;
                    NoProxy = true; // 修改为默认禁用系统代理
                    ProxyAddress = "";
                    EnableBinaryMerge = false; // 修改为默认不启用二进制合并
                    EnableDelAfterDone = true;
                    EnableMuxFastStart = true;
                    DisableDateInfo = false;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"重置设置时出错：{ex.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 选择文件夹
        /// </summary>
        private void BrowseFolder(object parameter)
        {
            try
            {
                // 使用文件夹浏览对话框
                string selectedPath = WindowsAPIFolderBrowser.ShowDialog(
                    "选择默认保存位置", 
                    !string.IsNullOrEmpty(DefaultSavePath) && Directory.Exists(DefaultSavePath) ? DefaultSavePath : null,
                    System.Windows.Application.Current.MainWindow
                );
                
                if (!string.IsNullOrWhiteSpace(selectedPath))
                {
                    DefaultSavePath = selectedPath;
                }
            }
            catch (Exception ex)
            {
                // 如果Windows API Code Pack失败，回退到标准对话框
                try
                {
                    using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                    {
                        dialog.Description = "选择默认保存位置";
                        dialog.ShowNewFolderButton = true;
                        
                        // 设置初始目录
                        if (!string.IsNullOrEmpty(DefaultSavePath) && Directory.Exists(DefaultSavePath))
                        {
                            dialog.SelectedPath = DefaultSavePath;
                        }
                        
                        // 尝试设置根文件夹为桌面以获得更好的使用体验
                        dialog.RootFolder = Environment.SpecialFolder.Desktop;
                        
                        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            DefaultSavePath = dialog.SelectedPath;
                        }
                    }
                }
                catch (Exception innerEx)
                {
                    System.Windows.MessageBox.Show($"选择文件夹时出错：{innerEx.Message}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
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
