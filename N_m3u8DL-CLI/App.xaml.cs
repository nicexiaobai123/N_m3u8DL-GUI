using System;
using System.Windows;
using N_m3u8DL_CLI.GUI;
using N_m3u8DL_CLI.NetCore;

namespace N_m3u8DL_CLI
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 当使用命令行参数启动时或强制使用控制台模式时，使用原始的命令行模式
            if ((e.Args != null && e.Args.Length > 0) || Global.ForceConsoleMode)
            {
                MyOptions options = new MyOptions();
                
                // 只有当有命令行参数时才处理参数
                if (e.Args != null && e.Args.Length > 0)
                {
                    // 设置Input参数
                    options.Input = e.Args[0];
                    
                    // 检查其他参数
                    for (int i = 1; i < e.Args.Length; i++)
                    {
                        string arg = e.Args[i];
                        if (arg.StartsWith("--") && i + 1 < e.Args.Length)
                        {
                            string optionName = arg.Substring(2);
                            string optionValue = e.Args[i + 1];
                            
                            // 根据参数名称设置对应的选项
                            // 这里只处理一些常用的，实际项目中可能需要完整实现
                            switch (optionName)
                            {
                                case "workDir":
                                    options.WorkDir = optionValue;
                                    break;
                                case "saveName":
                                    options.SaveName = optionValue;
                                    break;
                                case "maxThreads":
                                    if (uint.TryParse(optionValue, out uint maxThreads))
                                        options.MaxThreads = maxThreads;
                                    break;
                                case "minThreads":
                                    if (uint.TryParse(optionValue, out uint minThreads))
                                        options.MinThreads = minThreads;
                                    break;
                            }
                            i++; // 跳过值参数
                        }
                        else if (arg.StartsWith("--"))
                        {
                            string optionName = arg.Substring(2);
                            
                            // 处理布尔参数
                            switch (optionName)
                            {
                                case "enableBinaryMerge":
                                    options.EnableBinaryMerge = true;
                                    break;
                                case "noProxy":
                                    options.NoProxy = true;
                                    break;
                                case "disableDateInfo":
                                    options.DisableDateInfo = true;
                                    break;
                            }
                        }
                    }
                }
                
                // 调用原始的Program.DoWork方法
                Program.DoWork(options);
                
                // 退出应用程序
                Current.Shutdown();
            }
            else
            {
                // 当没有命令行参数且不强制使用控制台模式时，显示GUI界面
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
            }

            // 设置应用程序的全局异常处理
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var exception = args.ExceptionObject as Exception;
                MessageBox.Show($"发生了未处理的异常：{exception?.Message}\n\n{exception?.StackTrace}", 
                               "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            // 设置UI线程未捕获异常处理
            this.DispatcherUnhandledException += (sender, args) =>
            {
                MessageBox.Show($"发生了未处理的UI异常：{args.Exception.Message}\n\n{args.Exception.StackTrace}", 
                               "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
        }
    }
}
