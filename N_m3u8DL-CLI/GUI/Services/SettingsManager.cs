using System;
using System.IO;
using System.Xml.Serialization;
using N_m3u8DL_CLI.GUI.Models;
using Newtonsoft.Json;

namespace N_m3u8DL_CLI.GUI.Services
{
    /// <summary>
    /// 设置管理器，负责保存和加载应用设置
    /// </summary>
    public class SettingsManager
    {
        private static readonly object _lockObj = new object();
        private static SettingsManager _instance;
        
        private string _settingsFilePath;
        
        /// <summary>
        /// 当前应用设置
        /// </summary>
        public AppSettings Settings { get; private set; }
        
        /// <summary>
        /// 获取SettingsManager的单例实例
        /// </summary>
        public static SettingsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lockObj)
                    {
                        if (_instance == null)
                        {
                            _instance = new SettingsManager();
                        }
                    }
                }
                return _instance;
            }
        }
        
        private SettingsManager()
        {
            // 设置文件存储在用户AppData目录下
            string appDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "N_m3u8DL-CLI");
                
            // 确保目录存在
            if (!Directory.Exists(appDataFolder))
            {
                Directory.CreateDirectory(appDataFolder);
            }
            
            _settingsFilePath = Path.Combine(appDataFolder, "settings.xml");
            
            // 加载设置
            LoadSettings();
        }
        
        /// <summary>
        /// 加载应用设置
        /// </summary>
        public void LoadSettings()
        {
            // 默认下载路径
            string defaultDownloadPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Download");
                
            // 确保下载目录存在
            if (!Directory.Exists(defaultDownloadPath))
            {
                Directory.CreateDirectory(defaultDownloadPath);
            }
            
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    using (FileStream fs = new FileStream(_settingsFilePath, FileMode.Open))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
                        Settings = (AppSettings)serializer.Deserialize(fs);
                    }
                }
                else
                {
                    // 如果设置文件不存在，创建默认设置
                    Settings = new AppSettings
                    {
                        DefaultSavePath = defaultDownloadPath,
                        MaxThreads = 32,
                        MinThreads = 16,
                        RetryCount = 15,
                        TimeOut = 10,
                        MaxSpeed = 0,  // 不限速
                        NoProxy = true, 
                        ProxyAddress = "",
                        EnableBinaryMerge = false, 
                        EnableDelAfterDone = true,
                        EnableMuxFastStart = true,
                        DisableDateInfo = false
                    };
                    
                    // 保存默认设置
                    SaveSettings();
                }
            }
            catch (Exception)
            {
                // 如果出现异常，创建默认设置
                Settings = new AppSettings
                {
                    DefaultSavePath = defaultDownloadPath,
                    MaxThreads = 32,
                    MinThreads = 16,
                    RetryCount = 15,
                    TimeOut = 10,
                    MaxSpeed = 0,  // 不限速
                    NoProxy = true, 
                    ProxyAddress = "",
                    EnableBinaryMerge = false, 
                    EnableDelAfterDone = true,
                    EnableMuxFastStart = true,
                    DisableDateInfo = false
                };
                
                // 确保保存目录存在
                if (!Directory.Exists(Settings.DefaultSavePath))
                {
                    try
                    {
                        Directory.CreateDirectory(Settings.DefaultSavePath);
                    }
                    catch
                    {
                        // 如果创建目录失败，使用临时目录
                        Settings.DefaultSavePath = Path.GetTempPath();
                    }
                }
            }
        }
        
        /// <summary>
        /// 保存应用设置
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                using (FileStream fs = new FileStream(_settingsFilePath, FileMode.Create))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(AppSettings));
                    serializer.Serialize(fs, Settings);
                }
            }
            catch (Exception)
            {
                // 处理保存异常
            }
        }
    }
    
    /// <summary>
    /// 应用设置模型
    /// </summary>
    [Serializable]
    public class AppSettings
    {
        /// <summary>
        /// 默认保存路径
        /// </summary>
        public string DefaultSavePath { get; set; }
        
        /// <summary>
        /// 最大线程数
        /// </summary>
        public int MaxThreads { get; set; }
        
        /// <summary>
        /// 最小线程数
        /// </summary>
        public int MinThreads { get; set; }
        
        /// <summary>
        /// 重试次数
        /// </summary>
        public int RetryCount { get; set; }
        
        /// <summary>
        /// 超时时间（秒）
        /// </summary>
        public int TimeOut { get; set; }
        
        /// <summary>
        /// 下载速度上限（KB/s，0表示不限制）
        /// </summary>
        public int MaxSpeed { get; set; }
        
        /// <summary>
        /// 禁用系统代理
        /// </summary>
        public bool NoProxy { get; set; }
        
        /// <summary>
        /// 自定义代理地址
        /// </summary>
        public string ProxyAddress { get; set; }
        
        /// <summary>
        /// 启用二进制合并
        /// </summary>
        public bool EnableBinaryMerge { get; set; }
        
        /// <summary>
        /// 下载完成后删除临时文件
        /// </summary>
        public bool EnableDelAfterDone { get; set; }
        
        /// <summary>
        /// 开启混流FastStart特性
        /// </summary>
        public bool EnableMuxFastStart { get; set; }
        
        /// <summary>
        /// 关闭混流中的日期写入
        /// </summary>
        public bool DisableDateInfo { get; set; }
    }
}
