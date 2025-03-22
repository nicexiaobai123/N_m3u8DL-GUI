using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using N_m3u8DL_CLI.GUI.Helpers;
using N_m3u8DL_CLI.GUI.Models;
using N_m3u8DL_CLI.GUI.Services;
using N_m3u8DL_CLI.GUI;
using System.Windows.Forms;

namespace N_m3u8DL_CLI.GUI.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private string _url;
        private string _savePath;
        private string _saveFileName;
        private DownloadTask _selectedTask;
        private bool _isDownloading;
        private string _statusMessage;

        public MainViewModel()
        {
            // 初始化命令
            AddDownloadCommand = new RelayCommand(AddDownload, CanAddDownload);
            PauseResumeCommand = new RelayCommand(PauseResume, CanPauseResume);
            DeleteTaskCommand = new RelayCommand(DeleteTask, CanDeleteTask);
            OpenFolderCommand = new RelayCommand(OpenFolder, CanOpenFolder);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            BrowseFolderCommand = new RelayCommand(BrowseFolder);
            
            // 初始化集合
            DownloadTasks = new ObservableCollection<DownloadTask>();
            
            // 设置默认保存路径为应用设置中的默认保存路径
            SavePath = SettingsManager.Instance.Settings.DefaultSavePath;
            
            // 加载保存的任务
            LoadSavedTasks();
            
            // 初始状态消息
            StatusMessage = "准备就绪";
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
                (AddDownloadCommand as RelayCommand)?.RaiseCanExecuteChanged();
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
        /// 当前选中的任务
        /// </summary>
        public DownloadTask SelectedTask
        {
            get => _selectedTask;
            set
            {
                _selectedTask = value;
                OnPropertyChanged();
                (PauseResumeCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (DeleteTaskCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (OpenFolderCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// 是否正在下载
        /// </summary>
        public bool IsDownloading
        {
            get => _isDownloading;
            set
            {
                _isDownloading = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 下载任务集合
        /// </summary>
        public ObservableCollection<DownloadTask> DownloadTasks { get; }

        /// <summary>
        /// 添加下载命令
        /// </summary>
        public ICommand AddDownloadCommand { get; }

        /// <summary>
        /// 暂停/继续命令
        /// </summary>
        public ICommand PauseResumeCommand { get; }

        /// <summary>
        /// 删除任务命令
        /// </summary>
        public ICommand DeleteTaskCommand { get; }

        /// <summary>
        /// 打开文件夹命令
        /// </summary>
        public ICommand OpenFolderCommand { get; }

        /// <summary>
        /// 打开设置命令
        /// </summary>
        public ICommand OpenSettingsCommand { get; }

        /// <summary>
        /// 浏览文件夹命令
        /// </summary>
        public ICommand BrowseFolderCommand { get; }

        private bool CanAddDownload(object parameter)
        {
            return !string.IsNullOrWhiteSpace(Url) && !string.IsNullOrWhiteSpace(SavePath);
        }

        private async void AddDownload(object parameter)
        {
            try
            {
                // 检查URL是否为m3u8链接
                if (!Url.ToLower().Contains(".m3u8") && !Url.ToLower().Contains("/m3u8"))
                {
                    ShowError("请输入有效的m3u8链接");
                    return;
                }

                // 确保保存目录存在
                if (!Directory.Exists(SavePath))
                {
                    try
                    {
                        Directory.CreateDirectory(SavePath);
                    }
                    catch (Exception ex)
                    {
                        ShowError($"创建保存目录失败：{ex.Message}");
                        return;
                    }
                }

                // 生成保存文件名
                string fileName = string.IsNullOrWhiteSpace(SaveFileName) 
                    ? $"video_{DateTime.Now:yyyyMMdd_HHmmss}" 
                    : SaveFileName;

                // 创建下载任务
                var task = new DownloadTask
                {
                    Url = Url,
                    Name = fileName,
                    SavePath = SavePath,
                    SaveFileName = SaveFileName
                };
                
                DownloadTasks.Add(task);
                SelectedTask = task;
                
                // 更新状态消息
                StatusMessage = "已添加下载任务...";
                
                // 清空输入框
                Url = string.Empty;
                SaveFileName = string.Empty;
                
                // 开始下载
                await DownloadService.Instance.StartDownloadAsync(task);
                
                // 保存任务列表
                SaveTasks();
            }
            catch (Exception ex)
            {
                ShowError($"添加下载任务时出错：{ex.Message}");
                StatusMessage = "添加下载失败";
            }
        }

        private bool CanPauseResume(object parameter)
        {
            var task = parameter as DownloadTask ?? SelectedTask;
            return task != null && (task.Status == DownloadTask.TaskStatus.Downloading || task.Status == DownloadTask.TaskStatus.Paused);
        }

        private async void PauseResume(object parameter)
        {
            var task = parameter as DownloadTask ?? SelectedTask;
            if (task == null)
                return;

            try
            {
                if (task.Status == DownloadTask.TaskStatus.Downloading)
                {
                    // 暂停下载
                    DownloadService.Instance.PauseDownload(task);
                    StatusMessage = $"已暂停 {task.Name}";
                }
                else if (task.Status == DownloadTask.TaskStatus.Paused)
                {
                    // 恢复下载
                    await DownloadService.Instance.ResumeDownloadAsync(task);
                    StatusMessage = $"已恢复 {task.Name}";
                }
                
                // 保存任务列表
                SaveTasks();
            }
            catch (Exception ex)
            {
                ShowError($"操作任务时出错：{ex.Message}");
            }
        }

        private bool CanDeleteTask(object parameter)
        {
            var task = parameter as DownloadTask ?? SelectedTask;
            return task != null && task.Status != DownloadTask.TaskStatus.Deleted;
        }

        private void DeleteTask(object parameter)
        {
            var task = parameter as DownloadTask ?? SelectedTask;
            if (task == null)
                return;

            try
            {
                // 确认是否删除
                var result = CustomMessageBox.Show("确定要删除任务 \"" + task.Name + "\" 吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                    return;

                // 删除任务
                DownloadService.Instance.DeleteDownload(task);
                
                // 如果是直接从列表中删除，则从集合中移除
                if (task.Status == DownloadTask.TaskStatus.Completed || task.Status == DownloadTask.TaskStatus.Failed)
                {
                    DownloadTasks.Remove(task);
                }
                
                StatusMessage = $"已删除任务 {task.Name}";
                
                // 保存任务列表
                SaveTasks();
            }
            catch (Exception ex)
            {
                ShowError($"删除任务时出错：{ex.Message}");
            }
        }

        private bool CanOpenFolder(object parameter)
        {
            var task = parameter as DownloadTask ?? SelectedTask;
            return task != null && Directory.Exists(task.SavePath);
        }

        private void OpenFolder(object parameter)
        {
            var task = parameter as DownloadTask ?? SelectedTask;
            if (task == null)
                return;

            try
            {
                DownloadService.Instance.OpenFolder(task);
            }
            catch (Exception ex)
            {
                ShowError($"打开文件夹时出错：{ex.Message}");
            }
        }

        private void OpenSettings(object parameter)
        {
            try
            {
                var settingsWindow = new SettingsWindow
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };
                settingsWindow.ShowDialog();
                
                // 更新保存路径
                SavePath = SettingsManager.Instance.Settings.DefaultSavePath;
            }
            catch (Exception ex)
            {
                ShowError($"打开设置窗口时出错：{ex.Message}");
            }
        }

        private void BrowseFolder(object parameter)
        {
            try
            {
                // 使用文件夹浏览对话框
                string selectedPath = WindowsAPIFolderBrowser.ShowDialog(
                    "选择保存位置", 
                    !string.IsNullOrEmpty(SavePath) && Directory.Exists(SavePath) ? SavePath : null,
                    System.Windows.Application.Current.MainWindow
                );
                
                if (!string.IsNullOrWhiteSpace(selectedPath))
                {
                    SavePath = selectedPath;
                }
            }
            catch (Exception ex)
            {
                // 如果Windows API Code Pack失败，回退到标准对话框
                try
                {
                    using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                    {
                        dialog.Description = "选择保存位置";
                        dialog.ShowNewFolderButton = true;
                        
                        // 设置初始目录
                        if (!string.IsNullOrEmpty(SavePath) && Directory.Exists(SavePath))
                        {
                            dialog.SelectedPath = SavePath;
                        }
                        
                        // 尝试设置根文件夹为桌面以获得更好的使用体验
                        dialog.RootFolder = Environment.SpecialFolder.Desktop;
                        
                        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            SavePath = dialog.SelectedPath;
                        }
                    }
                }
                catch (Exception innerEx)
                {
                    ShowError($"选择文件夹时出错：{innerEx.Message}");
                }
            }
        }

        /// <summary>
        /// 加载已保存的下载任务
        /// </summary>
        private void LoadSavedTasks()
        {
            try
            {
                string tasksFilePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "N_m3u8DL-CLI", "tasks.json");

                if (File.Exists(tasksFilePath))
                {
                    var json = File.ReadAllText(tasksFilePath);
                    var tasks = System.Text.Json.JsonSerializer.Deserialize<List<DownloadTask>>(json);
                    
                    if (tasks != null && tasks.Count > 0)
                    {
                        foreach (var task in tasks)
                        {
                            // 只加载未完成或最近完成的任务（30天内）
                            if (task.Status == DownloadTask.TaskStatus.Paused || 
                                (task.Status == DownloadTask.TaskStatus.Completed && 
                                 (DateTime.Now - task.EndTime).TotalDays < 30))
                            {
                                DownloadTasks.Add(task);
                            }
                        }
                        
                        StatusMessage = $"已加载 {DownloadTasks.Count} 个任务";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"加载保存的任务失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 保存下载任务
        /// </summary>
        private void SaveTasks()
        {
            try
            {
                string appDataFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "N_m3u8DL-CLI");

                // 确保目录存在
                if (!Directory.Exists(appDataFolder))
                {
                    Directory.CreateDirectory(appDataFolder);
                }

                string tasksFilePath = Path.Combine(appDataFolder, "tasks.json");
                
                // 序列化为JSON
                var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                string json = System.Text.Json.JsonSerializer.Serialize(DownloadTasks, options);
                
                // 写入文件
                File.WriteAllText(tasksFilePath, json);
            }
            catch (Exception ex)
            {
                StatusMessage = $"保存任务失败: {ex.Message}";
            }
        }

        /// <summary>
        /// 显示错误消息框
        /// </summary>
        private void ShowError(string message)
        {
            CustomMessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
