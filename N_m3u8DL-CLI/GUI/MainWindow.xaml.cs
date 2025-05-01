using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using N_m3u8DL_CLI.GUI.Models;
using N_m3u8DL_CLI.GUI.ViewModels;

namespace N_m3u8DL_CLI.GUI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        
        /// <summary>
        /// 处理键盘按键事件，支持左右箭头键导航
        /// </summary>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // 获取ViewModel实例
            if (DataContext is MainViewModel viewModel)
            {
                // 处理方向键导航
                if (e.Key == Key.Left || e.Key == Key.Right || 
                    e.Key == Key.PageUp || e.Key == Key.PageDown)
                {
                    viewModel.HandleKeyNavigation(e.Key);
                    
                    // 标记事件已处理
                    e.Handled = true;
                }
            }
        }
    }
}
