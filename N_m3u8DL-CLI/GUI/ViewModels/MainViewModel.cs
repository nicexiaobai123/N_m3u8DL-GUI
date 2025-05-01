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
        private bool _isAllTasksPaused = false;
        
        // 分页相关字段
        private int _pageSize = 5; // 每页显示任务数
        private int _currentPage = 1; // 当前页码
        private ObservableCollection<DownloadTask> _pagedDownloadTasks; // 当前页的任务
        private ObservableCollection<PageNumber> _pageNumbers; // 页码列表

        public MainViewModel()
        {
            // 初始化命令
            AddDownloadCommand = new RelayCommand(AddDownload, CanAddDownload);
            PauseResumeCommand = new RelayCommand(PauseResume, CanPauseResume);
            DeleteTaskCommand = new RelayCommand(DeleteTask, CanDeleteTask);
            OpenFolderCommand = new RelayCommand(OpenFolder, CanOpenFolder);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            BrowseFolderCommand = new RelayCommand(BrowseFolder);
            ClearAllTasksCommand = new RelayCommand(ClearAllTasks, CanClearAllTasks);
            PauseResumeAllCommand = new RelayCommand(PauseResumeAll, CanPauseResumeAll);
            NextPageCommand = new RelayCommand(NextPage, CanNextPage);
            PreviousPageCommand = new RelayCommand(PreviousPage, CanPreviousPage);
            GoToPageCommand = new RelayCommand(GoToPage);
            
            // 初始化集合
            DownloadTasks = new ObservableCollection<DownloadTask>();
            PagedDownloadTasks = new ObservableCollection<DownloadTask>();
            PageNumbers = new ObservableCollection<PageNumber>();
            
            // 设置默认保存路径为应用设置中的默认保存路径
            SavePath = SettingsManager.Instance.Settings.DefaultSavePath;
            
            // 加载保存的任务
            LoadSavedTasks();
            
            // 初始化分页显示
            UpdatePagedTasks();
            
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
                // 取消订阅旧任务的状态变化事件
                if (_selectedTask != null)
                {
                    _selectedTask.StatusChanged -= SelectedTask_StatusChanged;
                }

                _selectedTask = value;
                OnPropertyChanged();
                (PauseResumeCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (DeleteTaskCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (OpenFolderCommand as RelayCommand)?.RaiseCanExecuteChanged();
                
                // 更新状态栏显示选中任务的状态
                if (value != null)
                {
                    // 订阅新任务的状态变化事件
                    value.StatusChanged += SelectedTask_StatusChanged;
                    UpdateStatusMessage(value);
                }
                else
                {
                    StatusMessage = "准备就绪";
                }
            }
        }
        
        /// <summary>
        /// 当选中任务的状态发生变化时更新状态栏
        /// </summary>
        private void SelectedTask_StatusChanged(object sender, EventArgs e)
        {
            if (sender is DownloadTask task)
            {
                UpdateStatusMessage(task);
            }
        }
        
        /// <summary>
        /// 更新状态栏消息
        /// </summary>
        private void UpdateStatusMessage(DownloadTask task)
        {
            // 创建一个详细的状态消息，包括任务名称、状态、进度、大小信息
            string statusInfo = $" {task.Name} - {task.Status}";
            
            // 添加进度信息（当下载中或暂停时显示）
            if (task.Status == DownloadTask.TaskStatus.Downloading || 
                task.Status == DownloadTask.TaskStatus.Paused)
            {
                statusInfo += $" - 进度: {task.Progress:F2}%";
                
                // 添加大小信息
                if (!string.IsNullOrEmpty(task.FormattedDownloadedSize) && 
                    !string.IsNullOrEmpty(task.FormattedTotalSize))
                {
                    statusInfo += $" | {task.FormattedDownloadedSize}/{task.FormattedTotalSize}";
                }
                
                // 添加速度信息
                if (task.Speed > 0 && 
                    task.Status == DownloadTask.TaskStatus.Downloading)
                {
                    statusInfo += $" | {task.FormattedSpeed}";
                }
            }
            
            StatusMessage = statusInfo;
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
        /// 所有下载任务集合
        /// </summary>
        public ObservableCollection<DownloadTask> DownloadTasks { get; }

        /// <summary>
        /// 当前页的下载任务集合
        /// </summary>
        public ObservableCollection<DownloadTask> PagedDownloadTasks
        {
            get => _pagedDownloadTasks;
            private set
            {
                _pagedDownloadTasks = value;
                OnPropertyChanged();
            }
        }
        
        /// <summary>
        /// 每页显示的任务数量
        /// </summary>
        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (_pageSize != value)
                {
                    _pageSize = value;
                    OnPropertyChanged();
                    UpdatePagedTasks();
                }
            }
        }
        
        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                if (_currentPage != value)
                {
                    _currentPage = value;
                    OnPropertyChanged();
                    UpdatePagedTasks();
                }
            }
        }
        
        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages
        {
            get => DownloadTasks.Count == 0 ? 1 : (int)Math.Ceiling((double)DownloadTasks.Count / PageSize);
        }
        
        /// <summary>
        /// 获取页码信息文本
        /// </summary>
        public string PageInfo
        {
            get => $"{CurrentPage}/{TotalPages}";
        }

        /// <summary>
        /// 下一页命令
        /// </summary>
        public ICommand NextPageCommand { get; }
        
        /// <summary>
        /// 上一页命令
        /// </summary>
        public ICommand PreviousPageCommand { get; }
        
        /// <summary>
        /// 跳转到指定页命令
        /// </summary>
        public ICommand GoToPageCommand { get; }
        
        /// <summary>
        /// 页码列表
        /// </summary>
        public ObservableCollection<PageNumber> PageNumbers
        {
            get => _pageNumbers;
            private set
            {
                _pageNumbers = value;
                OnPropertyChanged();
            }
        }

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

/// <summary>
/// 清除所有下载任务的命令
/// </summary>
public ICommand ClearAllTasksCommand { get; }

/// <summary>
/// 批量暂停/继续所有下载任务的命令
/// </summary>
public ICommand PauseResumeAllCommand { get; }

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
        
        // 更新分页显示
        UpdatePagedTasks();
        // 确保新添加的任务显示在当前页
        EnsureTaskVisible(task);
        
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

private async void DeleteTask(object parameter)
{
    var task = parameter as DownloadTask ?? SelectedTask;
    if (task == null)
        return;

    try
    {
        // 确认是否删除
        var result = CustomMessageBox.Show("确定要删除任务 \"" + task.Name + "\" 吗？\n删除后将清除所有下载临时文件。", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.No)
            return;

        // 更新状态消息
        StatusMessage = $"正在删除任务 {task.Name}...";
        
        // 删除任务和临时文件
        await DownloadService.Instance.DeleteDownloadAsync(task);
        
        // 删除完成后从任务列表中移除该任务
        DownloadTasks.Remove(task);
        
        // 更新分页显示
        UpdatePagedTasks();
        
        // 选择新的任务（如果有）
        if (PagedDownloadTasks.Count > 0)
        {
            SelectedTask = PagedDownloadTasks[0];
        }
        else if (DownloadTasks.Count > 0)
        {
            // 如果当前页没有任务但总任务列表有，可能需要切换到前一页
            if (CurrentPage > 1)
            {
                CurrentPage--;
            }
            SelectedTask = PagedDownloadTasks.FirstOrDefault();
        }
        else
        {
            SelectedTask = null;
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
/// 检查是否可以清除所有任务
/// </summary>
private bool CanClearAllTasks(object parameter)
{
    return DownloadTasks != null && DownloadTasks.Count > 0;
}

/// <summary>
/// 清除所有下载任务
/// </summary>
private async void ClearAllTasks(object parameter)
{
    if (DownloadTasks.Count == 0)
        return;
        
    var result = CustomMessageBox.Show(
        "确定要清除所有下载任务吗？",
        "确认操作",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question);
        
    if (result == MessageBoxResult.Yes)
    {
        try
        {
            // 保存当前任务数量用于状态显示
            int taskCount = DownloadTasks.Count;
            
            // 创建任务列表的副本，因为在循环中会修改集合
            var tasksToDelete = DownloadTasks.ToList();
            
            // 批量删除所有任务
            foreach (var task in tasksToDelete)
            {
                try
                {
                    // 如果任务正在下载中，先尝试暂停
                    if (task.Status == DownloadTask.TaskStatus.Downloading)
                    {
                        await Task.Run(() => DownloadService.Instance.PauseDownload(task));
                    }
                    
                    // 删除任务
                    await DownloadService.Instance.DeleteDownloadAsync(task);
                    
                    // 从集合中移除
                    DownloadTasks.Remove(task);
                }
                catch (Exception ex)
                {
                    task.AddToLog($"删除任务时出错: {ex.Message}");
                }
            }
            
            // 保存任务状态
            SaveTasks();
            
            // 更新分页显示
            UpdatePagedTasks();
            
            // 更新状态栏
            StatusMessage = $"已清除所有任务 ({taskCount}个)";
            
            // 刷新命令可执行状态
            (ClearAllTasksCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (PauseResumeAllCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
        catch (Exception ex)
        {
            ShowError($"清除所有任务时出错：{ex.Message}");
        }
    }
}

/// <summary>
/// 检查是否可以批量暂停/继续所有任务
/// </summary>
private bool CanPauseResumeAll(object parameter)
{
    // 只要有任务存在且有任务状态为下载中或暂停，就可以执行
    return DownloadTasks != null && DownloadTasks.Count > 0 && 
          (DownloadTasks.Any(t => t.Status == DownloadTask.TaskStatus.Downloading) || 
           DownloadTasks.Any(t => t.Status == DownloadTask.TaskStatus.Paused));
}

/// <summary>
/// 批量暂停/继续所有下载任务
/// </summary>
private async void PauseResumeAll(object parameter)
{
    if (DownloadTasks.Count == 0)
        return;
        
    try
    {
        // 检查是否有正在下载的任务
        bool hasDownloadingTasks = DownloadTasks.Any(t => t.Status == DownloadTask.TaskStatus.Downloading);
        
        // 根据是否有正在下载的任务决定操作是批量暂停还是批量继续
        if (hasDownloadingTasks)
        {
            // 批量暂停
            foreach (var task in DownloadTasks)
            {
                if (task.Status == DownloadTask.TaskStatus.Downloading)
                {
                    await Task.Run(() => DownloadService.Instance.PauseDownload(task));
                }
            }
            _isAllTasksPaused = true;
            StatusMessage = "已暂停所有下载任务";
        }
        else
        {
            // 批量继续
            foreach (var task in DownloadTasks)
            {
                if (task.Status == DownloadTask.TaskStatus.Paused)
                {
                    await DownloadService.Instance.ResumeDownloadAsync(task);
                }
            }
            _isAllTasksPaused = false;
            StatusMessage = "已继续所有下载任务";
        }
        
        // 刷新命令可执行状态
        (PauseResumeAllCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }
    catch (Exception ex)
    {
        ShowError($"批量操作任务时出错：{ex.Message}");
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

        #region 分页相关方法
        
        /// <summary>
        /// 更新分页显示的任务列表
        /// </summary>
        private void UpdatePagedTasks()
        {
            // 清空当前页任务
            PagedDownloadTasks.Clear();
            
            // 计算页码范围
            int totalPages = TotalPages;
            if(CurrentPage > totalPages && totalPages > 0)
            {
                CurrentPage = totalPages;
            }
            
            // 计算当前页面的任务范围
            int startIndex = (CurrentPage - 1) * PageSize;
            int endIndex = Math.Min(startIndex + PageSize, DownloadTasks.Count);
            
            // 添加当前页的任务
            for (int i = startIndex; i < endIndex; i++)
            {
                PagedDownloadTasks.Add(DownloadTasks[i]);
            }
            
            // 更新页码控件
            UpdatePageNumbers();
            
            // 通知属性变更
            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(PageInfo));
            
            // 更新翻页按钮状态
            (NextPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (PreviousPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
        
        /// <summary>
        /// 更新页码列表
        /// </summary>
        private void UpdatePageNumbers()
        {
            PageNumbers.Clear();
            int totalPages = TotalPages;
            
            // 如果总页数小于等于10，则显示所有页码
            if (totalPages <= 10)
            {
                for (int i = 1; i <= totalPages; i++)
                {
                    PageNumbers.Add(new PageNumber
                    {
                        Number = i,
                        IsCurrentPage = i == CurrentPage
                    });
                }
            }
            else
            {
                // 总是显示第一页
                PageNumbers.Add(new PageNumber
                {
                    Number = 1,
                    IsCurrentPage = CurrentPage == 1
                });
                
                // 当前页附近的页码
                int startPage = Math.Max(2, CurrentPage - 2);
                int endPage = Math.Min(totalPages - 1, CurrentPage + 2);
                
                // 如果前面缺失页码，添加省略号
                if (startPage > 2)
                {
                    PageNumbers.Add(new PageNumber
                    {
                        Number = 0,
                        IsEllipsis = true
                    });
                }
                
                // 添加中间的页码
                for (int i = startPage; i <= endPage; i++)
                {
                    PageNumbers.Add(new PageNumber
                    {
                        Number = i,
                        IsCurrentPage = i == CurrentPage
                    });
                }
                
                // 如果后面缺失页码，添加省略号
                if (endPage < totalPages - 1)
                {
                    PageNumbers.Add(new PageNumber
                    {
                        Number = 0,
                        IsEllipsis = true
                    });
                }
                
                // 总是显示最后一页
                PageNumbers.Add(new PageNumber
                {
                    Number = totalPages,
                    IsCurrentPage = CurrentPage == totalPages
                });
            }
        }
        
        /// <summary>
        /// 确保指定任务在当前页面显示
        /// </summary>
        private void EnsureTaskVisible(DownloadTask task)
        {
            if (task == null) return;
            
            int taskIndex = DownloadTasks.IndexOf(task);
            if (taskIndex < 0) return;
            
            int taskPage = (taskIndex / PageSize) + 1;
            if (CurrentPage != taskPage)
            {
                CurrentPage = taskPage;
            }
        }
        
        /// <summary>
        /// 检查是否可以跳转到下一页
        /// </summary>
        private bool CanNextPage(object parameter)
        {
            return CurrentPage < TotalPages;
        }
        
        /// <summary>
        /// 跳转到下一页
        /// </summary>
        private void NextPage(object parameter)
        {
            if (CanNextPage(parameter))
            {
                CurrentPage++;
            }
        }
        
        /// <summary>
        /// 检查是否可以跳转到上一页
        /// </summary>
        private bool CanPreviousPage(object parameter)
        {
            return CurrentPage > 1;
        }
        
        /// <summary>
        /// 跳转到上一页
        /// </summary>
        private void PreviousPage(object parameter)
        {
            if (CanPreviousPage(parameter))
            {
                CurrentPage--;
            }
        }
        
        /// <summary>
        /// 跳转到指定页
        /// </summary>
        private void GoToPage(object parameter)
        {
            if (parameter is int pageNumber && pageNumber > 0 && pageNumber <= TotalPages)
            {
                CurrentPage = pageNumber;
            }
        }
        
        /// <summary>
        /// 处理键盘导航
        /// </summary>
        public void HandleKeyNavigation(System.Windows.Input.Key key)
        {
            if (key == System.Windows.Input.Key.Left || key == System.Windows.Input.Key.PageUp)
            {
                PreviousPage(null);
            }
            else if (key == System.Windows.Input.Key.Right || key == System.Windows.Input.Key.PageDown)
            {
                NextPage(null);
            }
        }
        
        #endregion
        
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
