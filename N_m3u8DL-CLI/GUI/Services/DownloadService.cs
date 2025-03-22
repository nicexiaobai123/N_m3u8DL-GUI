using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
                        
                        // 如果用户指定了保存文件名
                        if (!string.IsNullOrEmpty(task.SaveFileName))
                        {
                            arguments.Add($"--saveName \"{task.SaveFileName}\"");
                        }
                        
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
        public void DeleteDownload(DownloadTask task)
        {
            if (task == null)
                return;

            // 如果任务正在下载，先暂停
            if (task.Status == DownloadTask.TaskStatus.Downloading)
            {
                PauseDownload(task);
            }

            task.Status = DownloadTask.TaskStatus.Deleted;
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
