using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using N_m3u8DL_CLI.GUI.Models;

namespace N_m3u8DL_CLI.GUI.Services
{
    /// <summary>
    /// 下载服务，用于连接GUI和原有的下载逻辑
    /// </summary>
    public class DownloadService
    {
        private static readonly object _lockObj = new object();
        private static DownloadService _instance;

        // 存储所有下载任务的字典
        private Dictionary<string, CancellationTokenSource> _downloadTokens;
        
        /// <summary>
        /// 获取DownloadService的单例实例
        /// </summary>
        public static DownloadService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObj)
                    {
                        if (_instance == null)
                        {
                            _instance = new DownloadService();
                        }
                    }
                }
                return _instance;
            }
        }

        private DownloadService()
        {
            _downloadTokens = new Dictionary<string, CancellationTokenSource>();
        }

        /// <summary>
        /// 开始下载任务
        /// </summary>
        /// <param name="task">下载任务</param>
        public async Task StartDownloadAsync(DownloadTask task)
        {
            // 任务已在运行
            if (_downloadTokens.ContainsKey(task.Id) && task.Status == DownloadTask.TaskStatus.Downloading)
                return;

            CancellationTokenSource cts = new CancellationTokenSource();
            _downloadTokens[task.Id] = cts;

            // 更新任务状态
            task.Status = DownloadTask.TaskStatus.Parsing;
            task.StartTime = DateTime.Now;

            try
            {
                // 使用TaskCompletionSource来异步执行命令行下载逻辑
                await Task.Run(() =>
                {
                    try
                    {
                        // 构建命令行参数
                        List<string> arguments = new List<string>();
                        
                        // 添加基本参数
                        arguments.Add($"--workDir \"{task.SavePath}\"");
                        
                        // 使用任务名称作为保存文件名，确保临时目录名与任务名一致
                        // 如果SaveFileName为空，则使用任务名
                        string saveName = !string.IsNullOrEmpty(task.SaveFileName) ? task.SaveFileName : task.Name;
                        arguments.Add($"--saveName \"{saveName}\"");
                        
                        // 从应用设置中获取其他参数
                        var settings = SettingsManager.Instance.Settings;
                        
                        if (settings.MinThreads > 0)
                            arguments.Add($"--minThreads {settings.MinThreads}");
                        
                        if (settings.MaxThreads > 0)
                            arguments.Add($"--maxThreads {settings.MaxThreads}");
                        
                        if (settings.TimeOut > 0)
                            arguments.Add($"--timeOut {settings.TimeOut}");
                        
                        if (settings.RetryCount > 0)
                            arguments.Add($"--retryCount {settings.RetryCount}");
                        
                        if (settings.MaxSpeed > 0)
                            arguments.Add($"--maxSpeed {settings.MaxSpeed}");
                        
                        if (settings.NoProxy)
                            arguments.Add("--noProxy");
                        
                        if (!string.IsNullOrEmpty(settings.ProxyAddress))
                            arguments.Add($"--proxyAddress {settings.ProxyAddress}");
                        
                        if (settings.EnableBinaryMerge)
                            arguments.Add("--enableBinaryMerge");
                        
                        if (settings.EnableDelAfterDone)
                            arguments.Add("--enableDelAfterDone");
                        
                        if (settings.EnableMuxFastStart)
                            arguments.Add("--enableMuxFastStart");
                        
                        if (settings.DisableDateInfo)
                            arguments.Add("--disableDateInfo");
                        
                        // 添加URL
                        arguments.Add($"\"{task.Url}\"");
                        
                        // 将重定向输出的处理逻辑封装在一个委托中
                        DataReceivedEventHandler outputHandler = (sender, e) =>
                        {
                            if (string.IsNullOrEmpty(e.Data))
                                return;
                            
                            string output = e.Data;
                            
                            // 更新状态和进度信息
                            UpdateTaskFromOutput(task, output);
                        };
                        
                        // 更新状态为下载中
                        task.Status = DownloadTask.TaskStatus.Downloading;
                        
                        // 创建进程来执行命令行下载
                        // 这里我们通过命令行调用自身，但传递参数
                        string exePath = Process.GetCurrentProcess().MainModule.FileName;
                        string args = string.Join(" ", arguments);
                        
                        using (Process process = new Process())
                        {
                            process.StartInfo.FileName = exePath;
                            process.StartInfo.Arguments = args;
                            process.StartInfo.UseShellExecute = false;
                            process.StartInfo.RedirectStandardOutput = true;
                            process.StartInfo.RedirectStandardError = true;
                            process.StartInfo.CreateNoWindow = true;
                            
                            process.OutputDataReceived += outputHandler;
                            process.ErrorDataReceived += outputHandler;
                            
                            process.Start();
                            process.BeginOutputReadLine();
                            process.BeginErrorReadLine();
                            
                            // 等待进程完成或被取消
                            while (!process.WaitForExit(500))
                            {
                                if (cts.Token.IsCancellationRequested)
                                {
                                    try
                                    {
                                        process.Kill();
                                        task.Status = DownloadTask.TaskStatus.Paused;
                                    }
                                    catch { }
                                    break;
                                }
                            }
                            
                            // 如果完成了且没有被取消
                            if (!cts.Token.IsCancellationRequested && task.Status != DownloadTask.TaskStatus.Failed)
                            {
                                task.Status = process.ExitCode == 0 ? 
                                    DownloadTask.TaskStatus.Completed : DownloadTask.TaskStatus.Failed;
                                
                                if (task.Status == DownloadTask.TaskStatus.Failed && string.IsNullOrEmpty(task.ErrorMessage))
                                {
                                    task.ErrorMessage = "下载失败，请检查链接或网络连接";
                                }
                                
                                task.EndTime = DateTime.Now;
                            }
                        }
                        
                    }
                    catch (Exception ex)
                    {
                        task.Status = DownloadTask.TaskStatus.Failed;
                        task.ErrorMessage = ex.Message;
                        task.EndTime = DateTime.Now;
                    }
                    finally
                    {
                        // 清理
                        _downloadTokens.Remove(task.Id);
                    }
                }, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // 操作被取消
                task.Status = DownloadTask.TaskStatus.Paused;
                _downloadTokens.Remove(task.Id);
            }
            catch (Exception ex)
            {
                // 其他异常
                task.Status = DownloadTask.TaskStatus.Failed;
                task.ErrorMessage = ex.Message;
                task.EndTime = DateTime.Now;
                _downloadTokens.Remove(task.Id);
            }
        }

        /// <summary>
        /// 从输出文本中更新任务信息
        /// </summary>
        private void UpdateTaskFromOutput(DownloadTask task, string output)
        {
            // 判断当前状态
            if (output.Contains("开始解析") || output.Contains("Starting"))
            {
                task.Status = DownloadTask.TaskStatus.Parsing;
            }
            else if (output.Contains("开始下载") || output.Contains("Start downloading"))
            {
                task.Status = DownloadTask.TaskStatus.Downloading;
            }
            else if (output.Contains("开始合并分片") || output.Contains("Start merging"))
            {
                task.Status = DownloadTask.TaskStatus.Merging;
            }
            else if (output.Contains("任务结束") || output.Contains("Task end"))
            {
                task.Status = DownloadTask.TaskStatus.Completed;
                task.Progress = 100;
                task.EndTime = DateTime.Now;
            }

            // 解析进度信息
            if (output.Contains("%"))
            {
                // 进度格式通常为：[下载进度 94.80%]
                Regex progressRegex = new Regex(@"\[.*?([\d\.]+)%\]");
                Match match = progressRegex.Match(output);
                if (match.Success && match.Groups.Count > 1)
                {
                    if (double.TryParse(match.Groups[1].Value, out double progress))
                    {
                        task.Progress = progress;
                    }
                }
                
                // 尝试解析已下载大小和总大小信息
                // 假设格式类似于: 已下载: 34.25 MB / 总大小: 100.00 MB
                Regex sizeRegex = new Regex(@"(\d+\.?\d*)\s*([KMGT]?B)\s*\/\s*(\d+\.?\d*)\s*([KMGT]?B)");
                match = sizeRegex.Match(output);
                if (match.Success && match.Groups.Count > 4)
                {
                    if (double.TryParse(match.Groups[1].Value, out double downloadedSize) &&
                        double.TryParse(match.Groups[3].Value, out double totalSize))
                    {
                        string downloadedUnit = match.Groups[2].Value;
                        string totalUnit = match.Groups[4].Value;
                        
                        // 将大小转换为字节
                        task.DownloadedSize = ConvertToBytes(downloadedSize, downloadedUnit);
                        task.TotalSize = ConvertToBytes(totalSize, totalUnit);
                        
                        // 使用已下载大小和总大小计算进度百分比
                        if (task.TotalSize > 0)
                        {
                            task.Progress = (double)task.DownloadedSize / task.TotalSize * 100;
                        }
                    }
                }
            }

            // 解析下载速度
            if (output.Contains("KB/s") || output.Contains("MB/s"))
            {
                Regex speedRegex = new Regex(@"([\d\.]+)\s*(KB/s|MB/s)");
                Match match = speedRegex.Match(output);
                if (match.Success && match.Groups.Count > 2)
                {
                    if (double.TryParse(match.Groups[1].Value, out double speedValue))
                    {
                        string unit = match.Groups[2].Value;
                        if (unit == "MB/s")
                            speedValue *= 1024; // 转换为KB/s

                        task.Speed = speedValue;
                    }
                }
            }

            // 解析出错信息
            if (output.Contains("错误") || output.Contains("Error"))
            {
                task.ErrorMessage = output;
            }

            // 添加到日志
            task.AddToLog(output);
        }
        
        /// <summary>
        /// 将带单位的大小转换为字节数
        /// </summary>
        private long ConvertToBytes(double size, string unit)
        {
            long bytes = 0;
            switch (unit.ToUpper())
            {
                case "B":
                    bytes = (long)size;
                    break;
                case "KB":
                    bytes = (long)(size * 1024);
                    break;
                case "MB":
                    bytes = (long)(size * 1024 * 1024);
                    break;
                case "GB":
                    bytes = (long)(size * 1024 * 1024 * 1024);
                    break;
                case "TB":
                    bytes = (long)(size * 1024 * 1024 * 1024 * 1024);
                    break;
                default:
                    bytes = (long)size;
                    break;
            }
            return bytes;
        }

        /// <summary>
        /// 暂停下载任务
        /// </summary>
        public void PauseDownload(DownloadTask task)
        {
            if (task == null || task.Status != DownloadTask.TaskStatus.Downloading)
                return;

            if (_downloadTokens.TryGetValue(task.Id, out CancellationTokenSource cts))
            {
                cts.Cancel();
                task.Status = DownloadTask.TaskStatus.Paused;
            }
        }

        /// <summary>
        /// 恢复下载任务
        /// </summary>
        public async Task ResumeDownloadAsync(DownloadTask task)
        {
            if (task == null || task.Status != DownloadTask.TaskStatus.Paused)
                return;

            await StartDownloadAsync(task);
        }

        /// <summary>
        /// 删除下载任务
        /// </summary>
        public async Task DeleteDownloadAsync(DownloadTask task)
        {
            if (task == null)
                return;

            // 如果任务正在下载，先暂停
            if (task.Status == DownloadTask.TaskStatus.Downloading)
            {
                PauseDownload(task);
                
                // 添加短暂延迟，确保下载进程完全停止并释放文件
                task.AddToLog("等待下载进程停止...");
                await Task.Delay(1000); // 等待1秒
            }
            
            // 设置为删除中状态
            task.Status = DownloadTask.TaskStatus.Deleting;
            task.AddToLog("删除中...");
            
            // 使用Task.Run在后台线程执行删除操作
            await Task.Run(() => {
                // 删除临时文件
                try
                {
                    // 获取任务的基本名称（不含状态信息）
                    string baseName = task.Name;
                    // 如果名称包含状态信息（如" - Paused"），则去除
                    int dashIndex = baseName.LastIndexOf(" - ");
                    if (dashIndex > 0)
                    {
                        baseName = baseName.Substring(0, dashIndex);
                    }
                    
                    // 首先查找与任务相关的所有可能的临时目录
                    var possibleTempDirs = Directory.GetDirectories(task.SavePath)
                        .Where(d => 
                        {
                            string dirName = Path.GetFileName(d);
                            // 检查目录名是否包含当前日期部分（例如，目录名以"_20230323"结尾）
                            // 或者目录名与任务名称完全匹配
                            string dateStr = task.StartTime.ToString("yyyyMMdd");
                            return dirName.EndsWith(dateStr, StringComparison.OrdinalIgnoreCase) || 
                                   dirName.Contains("_" + dateStr) ||
                                   dirName.Equals(baseName, StringComparison.OrdinalIgnoreCase);
                        })
                        .ToList();
                    
                    // 如果找不到可能的临时目录，则尝试使用任务名称作为临时目录名
                    if (!possibleTempDirs.Any())
                    {
                        possibleTempDirs.Add(Path.Combine(task.SavePath, baseName));
                    }
                    
                    // 尝试删除所有找到的可能的临时目录
                    foreach (var tempDir in possibleTempDirs)
                    {
                        if (Directory.Exists(tempDir))
                        {
                            task.AddToLog($"正在删除临时目录: {tempDir}");
                            
                            // 尝试多次删除，以应对文件可能被锁定的情况
                            int retryCount = 3;
                            bool deleted = false;
                            
                            for (int i = 0; i < retryCount && !deleted; i++)
                            {
                                try
                                {
                                    Directory.Delete(tempDir, true);
                                    deleted = true;
                                    task.AddToLog($"已删除临时目录: {tempDir}");
                                }
                                catch (IOException)
                                {
                                    // 如果文件被占用，等待一会再试
                                    if (i < retryCount - 1)
                                    {
                                        task.AddToLog($"临时目录被占用，等待后重试...");
                                        Thread.Sleep(1000); // 等待1秒
                                    }
                                    else
                                    {
                                        throw; // 最后一次尝试失败后抛出异常
                                    }
                                }
                            }
                        }
                        else
                        {
                            task.AddToLog($"未找到临时目录: {tempDir}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 记录删除失败，但继续执行
                    task.AddToLog($"删除临时文件时出错: {ex.Message}");
                }
            });

            // 删除完成，设置状态为已删除
            task.Status = DownloadTask.TaskStatus.Deleted;
            task.AddToLog("删除完成");
        }

        /// <summary>
        /// 删除下载任务（同步版本，保持向后兼容）
        /// </summary>
        public void DeleteDownload(DownloadTask task)
        {
            DeleteDownloadAsync(task).Wait();
        }

        /// <summary>
        /// 打开下载文件所在文件夹
        /// </summary>
        public void OpenFolder(DownloadTask task)
        {
            if (task == null || string.IsNullOrEmpty(task.SavePath))
                return;

            try
            {
                if (Directory.Exists(task.SavePath))
                {
                    Process.Start("explorer.exe", task.SavePath);
                }
            }
            catch (Exception)
            {
                // 处理异常
            }
        }
    }
}
