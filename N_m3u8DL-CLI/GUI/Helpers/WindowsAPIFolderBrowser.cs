using System;
using System.IO;
using System.Diagnostics;
using System.Windows;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using N_m3u8DL_CLI;

namespace N_m3u8DL_CLI.GUI.Helpers
{
    /// <summary>
    /// 文件夹选择对话框
    /// </summary>
    public static class WindowsAPIFolderBrowser
    {
        /// <summary>
        /// 显示文件夹选择对话框
        /// </summary>
        /// <param name="title">对话框标题</param>
        /// <param name="initialFolder">初始文件夹路径</param>
        /// <param name="ownerWindow">父窗口</param>
        /// <returns>选择的文件夹路径，如果用户取消则返回null</returns>
        public static string ShowDialog(string title, string initialFolder, Window ownerWindow)
        {
            LogInfo($"显示文件夹对话框...");
            LogInfo($"标题: {title}");
            LogInfo($"初始文件夹: {initialFolder}");
            
            try
            {
                // 尝试使用Windows 10风格的对话框
                return ShowModernDialog(title, initialFolder, ownerWindow);
            }
            catch (Exception ex)
            {
                LogError("显示现代风格对话框时发生异常", ex);
                
                // 出错时回退到传统对话框
                LogInfo("尝试使用传统对话框作为备选方案");
                return ShowLegacyDialog(title, initialFolder);
            }
        }
        
        /// <summary>
        /// 显示Windows 10风格的文件夹选择对话框
        /// </summary>
        private static string ShowModernDialog(string title, string initialFolder, Window ownerWindow)
        {
            // 使用Windows COM接口实现现代风格文件夹选择
            IFileOpenDialog dialog = (IFileOpenDialog)new FileOpenDialog();
            
            try
            {
                // 设置对话框选项 - FOS_PICKFOLDERS意味着只选择文件夹
                dialog.SetOptions(FOS.FOS_PICKFOLDERS | FOS.FOS_FORCEFILESYSTEM | FOS.FOS_PATHMUSTEXIST);
                
                // 设置标题
                dialog.SetTitle(title);
                
                // 尝试设置初始目录
                if (!string.IsNullOrEmpty(initialFolder) && Directory.Exists(initialFolder))
                {
                    // 将路径字符串转换为PIDL
                    IShellItem folder;
                    SHCreateItemFromParsingName(initialFolder, IntPtr.Zero, typeof(IShellItem).GUID, out folder);
                    
                    if (folder != null)
                    {
                        dialog.SetFolder(folder);
                        LogInfo($"设置初始路径: {initialFolder}");
                        Marshal.ReleaseComObject(folder);
                    }
                }
                
                // 显示对话框 - 获取父窗口句柄
                IntPtr hwndOwner = IntPtr.Zero;
                if (ownerWindow != null)
                {
                    hwndOwner = new System.Windows.Interop.WindowInteropHelper(ownerWindow).Handle;
                }
                
                uint hr = dialog.Show(hwndOwner);
                
                // 如果用户选择了文件夹
                if (hr == ERROR_OK)
                {
                    IShellItem shellItem;
                    dialog.GetResult(out shellItem);
                    
                    string selectedPath;
                    shellItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out selectedPath);
                    
                    Marshal.ReleaseComObject(shellItem);
                    Marshal.ReleaseComObject(dialog);
                    
                    LogInfo($"用户选择了文件夹: {selectedPath}");
                    return selectedPath;
                }
                else
                {
                    // 用户取消
                    Marshal.ReleaseComObject(dialog);
                    LogInfo("用户取消了选择");
                    return null;
                }
            }
            catch (Exception)
            {
                // 确保COM对象被释放
                if (dialog != null)
                {
                    Marshal.ReleaseComObject(dialog);
                }
                throw; // 重新抛出异常以便回退到传统对话框
            }
        }
        
        /// <summary>
        /// 显示传统的文件夹选择对话框（用作备用方案 - 适用于Win7等旧系统）
        /// </summary>
        private static string ShowLegacyDialog(string title, string initialFolder)
        {
            try
            {
                using (var dialog = new FolderBrowserDialog())
                {
                    dialog.Description = title;
                    dialog.ShowNewFolderButton = true;
                    
                    // 设置初始目录
                    if (!string.IsNullOrEmpty(initialFolder) && Directory.Exists(initialFolder))
                    {
                        dialog.SelectedPath = initialFolder;
                        LogInfo($"[传统对话框] 设置初始路径: {initialFolder}");
                    }
                    
                    // 设置根文件夹为桌面以获得更好的使用体验
                    dialog.RootFolder = Environment.SpecialFolder.Desktop;
                    
                    // 显示对话框
                    DialogResult result = dialog.ShowDialog();
                    
                    if (result == DialogResult.OK)
                    {
                        string selectedPath = dialog.SelectedPath;
                        LogInfo($"[传统对话框] 用户选择了文件夹: {selectedPath}");
                        return selectedPath;
                    }
                    
                    LogInfo("[传统对话框] 用户取消了选择");
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogError("[传统对话框] 显示文件夹对话框时发生异常", ex);
                return null;
            }
        }
        
        #region 日志方法
        
        private static void LogInfo(string message)
        {
            try
            {
                Debug.WriteLine(message);
                LOGGER.WriteLine(message);
            }
            catch { /* 忽略日志错误 */ }
        }
        
        private static void LogError(string message, Exception ex = null)
        {
            try
            {
                Debug.WriteLine($"ERROR: {message}");
                
                if (ex != null)
                {
                    Debug.WriteLine($"Exception: {ex.Message}");
                    Debug.WriteLine($"StackTrace: {ex.StackTrace}");
                    
                    LOGGER.WriteLine($"[ERROR] {message}");
                    LOGGER.WriteLine($"[ERROR] 异常类型: {ex.GetType().FullName}");
                    LOGGER.WriteLine($"[ERROR] 异常消息: {ex.Message}");
                    
                    if (ex.InnerException != null)
                    {
                        LOGGER.WriteLine($"[ERROR] 内部异常: {ex.InnerException.Message}");
                    }
                    
                    LOGGER.WriteLine($"[ERROR] 堆栈跟踪: {ex.StackTrace}");
                }
                else
                {
                    LOGGER.WriteLine($"[ERROR] {message}");
                }
            }
            catch { /* 忽略日志错误 */ }
        }
        
        #endregion
        
        #region Windows COM接口定义
        
        // COM接口和常量定义，用于实现Windows 10样式的文件夹选择对话框
        
        [ComImport, Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
        private class FileOpenDialog { }
        
        [ComImport, Guid("42f85136-db7e-439c-85f1-e4075d135fc8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog
        {
            [PreserveSig]
            uint Show(IntPtr hwndParent);
            void SetFileTypes();
            void SetFileTypeIndex(uint iFileType);
            void GetFileTypeIndex(out uint piFileType);
            void Advise();
            void Unadvise();
            void SetOptions(FOS fos);
            void GetOptions(out FOS pfos);
            void SetDefaultFolder(IShellItem psi);
            void SetFolder(IShellItem psi);
            void GetFolder(out IShellItem ppsi);
            void GetCurrentSelection(out IShellItem ppsi);
            void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
            void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
            void GetResult(out IShellItem ppsi);
            void AddPlace(IShellItem psi, int alignment);
            void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
            void Close(int hr);
            void SetClientGuid();
            void ClearClientData();
            void SetFilter([MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);
            void GetResults([MarshalAs(UnmanagedType.Interface)] out IntPtr ppenum);
            void GetSelectedItems([MarshalAs(UnmanagedType.Interface)] out IntPtr ppsai);
        }
        
        [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler();
            void GetParent();
            void GetDisplayName(SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
            void GetAttributes();
            void Compare();
        }
        
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            IntPtr pbc,
            [MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);
        
        private enum SIGDN : uint
        {
            SIGDN_NORMALDISPLAY = 0,
            SIGDN_PARENTRELATIVEPARSING = 0x80018001,
            SIGDN_DESKTOPABSOLUTEPARSING = 0x80028000,
            SIGDN_PARENTRELATIVEEDITING = 0x80031001,
            SIGDN_DESKTOPABSOLUTEEDITING = 0x8004c000,
            SIGDN_FILESYSPATH = 0x80058000,
            SIGDN_URL = 0x80068000,
            SIGDN_PARENTRELATIVEFORADDRESSBAR = 0x8007c001,
            SIGDN_PARENTRELATIVE = 0x80080001
        }
        
        [Flags]
        private enum FOS : uint
        {
            FOS_OVERWRITEPROMPT = 0x00000002,
            FOS_STRICTFILETYPES = 0x00000004,
            FOS_NOCHANGEDIR = 0x00000008,
            FOS_PICKFOLDERS = 0x00000020,
            FOS_FORCEFILESYSTEM = 0x00000040,
            FOS_ALLNONSTORAGEITEMS = 0x00000080,
            FOS_NOVALIDATE = 0x00000100,
            FOS_ALLOWMULTISELECT = 0x00000200,
            FOS_PATHMUSTEXIST = 0x00000800,
            FOS_FILEMUSTEXIST = 0x00001000,
            FOS_CREATEPROMPT = 0x00002000,
            FOS_SHAREAWARE = 0x00004000,
            FOS_NOREADONLYRETURN = 0x00008000,
            FOS_NOTESTFILECREATE = 0x00010000,
            FOS_HIDEMRUPLACES = 0x00020000,
            FOS_HIDEPINNEDPLACES = 0x00040000,
            FOS_NODEREFERENCELINKS = 0x00100000,
            FOS_DONTADDTORECENT = 0x02000000,
            FOS_FORCESHOWHIDDEN = 0x10000000,
            FOS_DEFAULTNOMINIMODE = 0x20000000
        }
        
        private const uint ERROR_OK = 0;
        
        #endregion
    }
}
