using System;
using System.Windows;
using N_m3u8DL_CLI.GUI;
using N_m3u8DL_CLI.NetCore;
using System.Net;
using System.Threading;
using System.Globalization;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using CommandLine;

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

            // 初始化应用程序的各项设置
            InitializeNetworkSettings();
            SetupLocalization();
            
            // 处理命令行参数
            string[] cmdArgs = e.Args;
            cmdArgs = ProcessSpecialProtocols(cmdArgs);
            
            // 寻找并验证FFmpeg
            if (!VerifyFFmpegExists())
            {
                Current.Shutdown();
                return;
            }
            
            // 检查更新
            StartUpdateCheck();
            
            // 设置ReadLine字数上限
            ConfigureConsoleInput();

            // 根据命令行参数和配置决定运行模式
            if ((cmdArgs != null && cmdArgs.Length > 0) || Global.ForceConsoleMode)
            {
                RunConsoleMode(cmdArgs);
            }
            else
            {
                RunGuiMode();
            }

            // 设置全局异常处理
            SetupExceptionHandlers();
        }

        #region 初始化方法

        /// <summary>
        /// 初始化网络设置，配置SSL证书验证和安全协议
        /// </summary>
        private void InitializeNetworkSettings()
        {
            // 配置服务点管理器 - 接受所有证书
            ServicePointManager.ServerCertificateValidationCallback = delegate (
                object sender,
                X509Certificate certificate,
                X509Chain chain,
                SslPolicyErrors sslPolicyErrors)
            {
                return true; // 接受所有证书
            };
            
            // 配置连接和安全设置
            ServicePointManager.DefaultConnectionLimit = 1024;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3
                               | SecurityProtocolType.Tls
                               | (SecurityProtocolType)0x300 // Tls11  
                               | (SecurityProtocolType)0xC00; // Tls12  
        }

        /// <summary>
        /// 设置应用程序的本地化语言
        /// </summary>
        private void SetupLocalization()
        {
            try
            {
                string loc = "en-US";
                string currLoc = Thread.CurrentThread.CurrentUICulture.Name;
                if (currLoc == "zh-TW" || currLoc == "zh-HK" || currLoc == "zh-MO") loc = "zh-TW";
                else if (currLoc == "zh-CN" || currLoc == "zh-SG") loc = "zh-CN";
                //设置语言
                CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(loc);
                Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(loc);
            }
            catch (Exception) {; }
        }

        /// <summary>
        /// 处理特殊URL协议参数
        /// </summary>
        /// <param name="args">命令行参数</param>
        /// <returns>处理后的命令行参数</returns>
        private string[] ProcessSpecialProtocols(string[] args)
        {
            if (args == null || args.Length != 1)
                return args;

            // 处理m3u8dl协议
            if (args[0].ToLower().StartsWith("m3u8dl:"))
            {
                var base64 = args[0].Replace("m3u8dl://", "").Replace("m3u8dl:", "");
                var cmd = "";
                try { cmd = Encoding.UTF8.GetString(Convert.FromBase64String(base64)); }
                catch (FormatException) { cmd = Encoding.UTF8.GetString(Convert.FromBase64String(base64.TrimEnd('/'))); }
                //修正参数转义符
                cmd = cmd.Replace("\\\"", "\"");
                //修正工作目录
                Environment.CurrentDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                return Global.ParseArguments(cmd).ToArray();  //解析命令行
            }
            // 处理URL协议注册/注销
            else if (args[0] == "--registerUrlProtocol")
            {
                try
                {
                    MessageBox.Show("注册URL协议需要管理员权限，请以管理员身份运行程序。", "需要权限", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("注册URL协议失败: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                Current.Shutdown();
                return args;
            }
            else if (args[0] == "--unregisterUrlProtocol")
            {
                try 
                {
                    MessageBox.Show("注销URL协议需要管理员权限，请以管理员身份运行程序。", "需要权限", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("注销URL协议失败: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                Current.Shutdown();
                return args;
            }

            return args;
        }

        /// <summary>
        /// 查找和验证FFmpeg是否存在
        /// </summary>
        /// <returns>FFmpeg是否存在并可用</returns>
        private bool VerifyFFmpegExists()
        {
            // 在当前目录查找
            if (File.Exists("ffmpeg.exe"))
            {
                FFmpeg.FFMPEG_PATH = Path.Combine(Environment.CurrentDirectory, "ffmpeg.exe");
                return true;
            }
            // 在程序所在目录查找
            else if (File.Exists(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "ffmpeg.exe")))
            {
                FFmpeg.FFMPEG_PATH = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "ffmpeg.exe");
                return true;
            }
            // 在环境变量中查找
            else
            {
                try
                {
                    string[] EnvironmentPath = Environment.GetEnvironmentVariable("Path").Split(';');
                    foreach (var de in EnvironmentPath)
                    {
                        if (File.Exists(Path.Combine(de.Trim('\"').Trim(), "ffmpeg.exe")))
                        {
                            FFmpeg.FFMPEG_PATH = Path.Combine(de.Trim('\"').Trim(), "ffmpeg.exe");
                            return true;
                        }
                    }
                }
                catch (Exception)
                {
                    ;
                }

                // 未找到FFmpeg，显示错误消息
                MessageBox.Show(strings.ffmpegLost + "\n" + strings.ffmpegTip + "\n\nhttp://ffmpeg.org/download.html#build-windows", 
                    "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// 启动更新检查线程
        /// </summary>
        private void StartUpdateCheck()
        {
            if (!File.Exists(Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "NO_UPDATE")))
            {
                Thread checkUpdate = new Thread(() =>
                {
                    Global.CheckUpdate();
                });
                checkUpdate.IsBackground = true;
                checkUpdate.Start();
            }
        }

        /// <summary>
        /// 配置控制台输入
        /// </summary>
        private void ConfigureConsoleInput()
        {
            Stream steam = Console.OpenStandardInput();
            Console.SetIn(new StreamReader(steam, Encoding.Default, false, 5000));
        }

        #endregion

        #region 运行模式

        /// <summary>
        /// 运行控制台模式
        /// </summary>
        /// <param name="cmdArgs">命令行参数</param>
        private void RunConsoleMode(string[] cmdArgs)
        {
            // 如果没有传入参数，则提示用户输入
            if (cmdArgs == null || cmdArgs.Length == 0)
            {
                cmdArgs = PromptForUserInput();
                if (cmdArgs == null) // 用户未输入任何内容
                {
                    Current.Shutdown();
                    return;
                }
            }

            // 处理配置文件
            cmdArgs = ProcessConfigFile(cmdArgs);

            // 使用CommandLine解析命令行参数
            var cmdParser = new CommandLine.Parser(with => with.HelpWriter = null);
            var parserResult = cmdParser.ParseArguments<MyOptions>(cmdArgs);

            // 解析命令行并执行相应的操作
            parserResult
                .WithParsed(o => Program.DoWork(o))
                .WithNotParsed(errs => 
                {
                    // 显示参数错误信息
                    Console.WriteLine("参数解析错误，请检查输入。");
                    Current.Shutdown();
                });
            
            // 退出应用程序
            Current.Shutdown();
        }

        /// <summary>
        /// 提示用户输入下载链接
        /// </summary>
        /// <returns>解析后的命令行参数</returns>
        private string[] PromptForUserInput()
        {
            Global.WriteInit();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("N_m3u8DL-CLI");
            Console.ResetColor();
            Console.Write(" > ");

            var cmd = Console.ReadLine();
            if (string.IsNullOrEmpty(cmd))
                return null;
                
            string[] args = Global.ParseArguments(cmd).ToArray();
            Console.Clear();
            return args;
        }

        /// <summary>
        /// 处理配置文件中的默认参数
        /// </summary>
        /// <param name="cmdArgs">命令行参数</param>
        /// <returns>处理后的命令行参数</returns>
        private string[] ProcessConfigFile(string[] cmdArgs)
        {
            if (cmdArgs.Length == 1 || (cmdArgs.Length == 3 && cmdArgs[1].ToLower() == "--savename"))
            {
                string configFilePath = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "N_m3u8DL-CLI.args.txt");
                if (File.Exists(configFilePath))
                {
                    if (cmdArgs.Length == 3)
                    {
                        return Global.ParseArguments($"\"{cmdArgs[0]}\" {cmdArgs[1]} {cmdArgs[2]} " + File.ReadAllText(configFilePath)).ToArray();
                    }
                    else
                    {
                        return Global.ParseArguments($"\"{cmdArgs[0]}\" " + File.ReadAllText(configFilePath)).ToArray();
                    }
                }
            }
            return cmdArgs;
        }

        /// <summary>
        /// 运行GUI模式
        /// </summary>
        private void RunGuiMode()
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }

        #endregion

        /// <summary>
        /// 设置应用程序的全局异常处理
        /// </summary>
        private void SetupExceptionHandlers()
        {
            // 设置应用程序域级别的异常处理
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
