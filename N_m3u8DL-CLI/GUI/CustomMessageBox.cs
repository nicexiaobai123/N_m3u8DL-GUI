using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace N_m3u8DL_CLI.GUI
{
    /// <summary>
    /// 自定义消息框
    /// </summary>
    public class CustomMessageBox
    {
        /// <summary>
        /// 显示一个消息框
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">标题</param>
        /// <param name="button">按钮类型</param>
        /// <param name="image">图标类型</param>
        /// <param name="owner">父窗口</param>
        /// <returns>点击的结果</returns>
        public static MessageBoxResult Show(string message, string title = "提示", 
            MessageBoxButton button = MessageBoxButton.OK, 
            MessageBoxImage image = MessageBoxImage.None, 
            Window owner = null)
        {
            var window = new Window
            {
                Title = title,
                Width = 400,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Owner = owner ?? Application.Current.MainWindow,
                WindowStyle = WindowStyle.None,  // 无边框样式
                AllowsTransparency = true,       // 允许透明
                Topmost = true                   // 置顶显示
            };

            // 获取应用资源
            var backgroundBrush = Application.Current.Resources["CardBackgroundBrush"] as SolidColorBrush ?? new SolidColorBrush(Colors.White);
            var foregroundBrush = Application.Current.Resources["ForegroundBrush"] as SolidColorBrush ?? new SolidColorBrush(Color.FromRgb(51, 51, 51));
            var primaryBrush = Application.Current.Resources["PrimaryBrush"] as SolidColorBrush ?? new SolidColorBrush(Color.FromRgb(52, 152, 219));

            // 创建主布局
            var mainBorder = new Border
            {
                Background = backgroundBrush,
                CornerRadius = new CornerRadius(3),       // 小的圆角
                BorderBrush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    ShadowDepth = 2,
                    BlurRadius = 5,
                    Opacity = 0.2
                }
            };

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // 标题行
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // 消息行
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });  // 按钮行

            // 标题栏
            var titleBar = new Grid { Margin = new Thickness(0, 0, 0, 15) };
            titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });  // 图标列
            titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });  // 标题列

            // 图标
            var iconEllipse = new Ellipse
            {
                Width = 32,
                Height = 32,
                Fill = GetIconBackgroundBrush(image),
                Margin = new Thickness(0, 0, 10, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };

            // 使用Canvas确保X标记完全居中
            var iconCanvas = new Canvas
            {
                Width = 32,
                Height = 32,
                Margin = new Thickness(-4, 0, 0, 0)  // 再向左偏移1个单位
            };

            // 叉号使用两条交叉线实现
            if (image == MessageBoxImage.Error)
            {
                // 两条交叉的线
                var line1 = new Line
                {
                    X1 = 7,
                    Y1 = 10,
                    X2 = 19,
                    Y2 = 22,
                    StrokeThickness = 2.0,
                    Stroke = Brushes.White
                };
                
                var line2 = new Line
                {
                    X1 = 19,
                    Y1 = 10,
                    X2 = 7,
                    Y2 = 22,
                    StrokeThickness = 2.0,
                    Stroke = Brushes.White
                };
                
                iconCanvas.Children.Add(line1);
                iconCanvas.Children.Add(line2);
            }
            else
            {
                // 其他图标仍然使用文字
                var iconText = new TextBlock
                {
                    Text = GetIconSymbol(image),
                    FontSize = 22,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    TextAlignment = TextAlignment.Center
                };
                
                Canvas.SetLeft(iconText, 11);
                Canvas.SetTop(iconText, 2);
                iconCanvas.Children.Add(iconText);
            }

            var iconContainer = new Grid();
            iconContainer.Children.Add(iconEllipse);
            iconContainer.Children.Add(iconCanvas);

            // 标题文本
            var titleBlock = new TextBlock
            {
                Text = title,
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = foregroundBrush,
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(iconContainer, 0);
            Grid.SetColumn(titleBlock, 1);
            titleBar.Children.Add(iconContainer);
            titleBar.Children.Add(titleBlock);

            // 消息内容
            var messageBlock = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 20),
                FontSize = 14,
                Foreground = foregroundBrush,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            // 按钮区域
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            MessageBoxResult result = MessageBoxResult.None;
            Button yesButton = null, noButton = null, okButton = null, cancelButton = null;

            // 根据按钮类型创建按钮
            switch (button)
            {
                case MessageBoxButton.YesNo:
                    yesButton = CreateModernButton("是", () => { result = MessageBoxResult.Yes; window.Close(); }, true);
                    noButton = CreateModernButton("否", () => { result = MessageBoxResult.No; window.Close(); }, false);
                    buttonPanel.Children.Add(noButton);
                    buttonPanel.Children.Add(new Border { Width = 8 }); // 间隔
                    buttonPanel.Children.Add(yesButton);
                    break;

                case MessageBoxButton.YesNoCancel:
                    yesButton = CreateModernButton("是", () => { result = MessageBoxResult.Yes; window.Close(); }, true);
                    noButton = CreateModernButton("否", () => { result = MessageBoxResult.No; window.Close(); }, false);
                    cancelButton = CreateModernButton("取消", () => { result = MessageBoxResult.Cancel; window.Close(); }, false);
                    buttonPanel.Children.Add(cancelButton);
                    buttonPanel.Children.Add(new Border { Width = 8 });
                    buttonPanel.Children.Add(noButton);
                    buttonPanel.Children.Add(new Border { Width = 8 });
                    buttonPanel.Children.Add(yesButton);
                    break;

                case MessageBoxButton.OKCancel:
                    okButton = CreateModernButton("确定", () => { result = MessageBoxResult.OK; window.Close(); }, true);
                    cancelButton = CreateModernButton("取消", () => { result = MessageBoxResult.Cancel; window.Close(); }, false);
                    buttonPanel.Children.Add(cancelButton);
                    buttonPanel.Children.Add(new Border { Width = 8 });
                    buttonPanel.Children.Add(okButton);
                    break;

                case MessageBoxButton.OK:
                default:
                    okButton = CreateModernButton("确定", () => { result = MessageBoxResult.OK; window.Close(); }, true);
                    buttonPanel.Children.Add(okButton);
                    break;
            }

            // 添加关闭按钮
            var closeButton = new Button
            {
                Width = 24,
                Height = 24,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, -10, -10, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0)
            };

            var closePath = new Path
            {
                Data = Geometry.Parse("M16 8.707l-7.146 7.146-1.414-1.414L14.586 7.293 7.439 0.146l1.414-1.414L16 5.879l7.146-7.146 1.414 1.414-7.146 7.146 7.146 7.146-1.414 1.414L16 8.707z"),
                Fill = new SolidColorBrush(Color.FromArgb(120, 0, 0, 0)),
                Width = 14,
                Height = 14,
                Stretch = Stretch.Uniform
            };

            closeButton.Content = closePath;
            closeButton.Click += (s, e) => { result = MessageBoxResult.Cancel; window.Close(); };

            // 组装界面
            Grid.SetRow(titleBar, 0);
            grid.Children.Add(titleBar);
            Grid.SetRow(messageBlock, 1);
            grid.Children.Add(messageBlock);
            Grid.SetRow(buttonPanel, 2);
            grid.Children.Add(buttonPanel);
            grid.Children.Add(closeButton);

            mainBorder.Child = grid;
            window.Content = mainBorder;

            // 添加拖动功能
            window.MouseLeftButtonDown += (s, e) => window.DragMove();

            // 显示窗口前设置动画
            window.Opacity = 0;
            
            // 显示对话框
            window.Show();
            
            // 添加淡入动画
            var fadeInAnimation = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(150))
            };
            window.BeginAnimation(UIElement.OpacityProperty, fadeInAnimation);

            // 创建一个模态阻塞，直到用户关闭窗口
            System.Windows.Threading.DispatcherFrame frame = new System.Windows.Threading.DispatcherFrame();
            window.Closed += (s, e) => frame.Continue = false;
            System.Windows.Threading.Dispatcher.PushFrame(frame);

            return result;
        }

        /// <summary>
        /// 根据消息框图标类型获取背景颜色
        /// </summary>
        private static Brush GetIconBackgroundBrush(MessageBoxImage image)
        {
            switch (image)
            {
                case MessageBoxImage.Error:
                    return Application.Current.Resources["ErrorBrush"] as Brush ?? 
                           new SolidColorBrush(Color.FromRgb(231, 76, 60)); // 红色
                case MessageBoxImage.Warning:
                    return Application.Current.Resources["WarningBrush"] as Brush ?? 
                           new SolidColorBrush(Color.FromRgb(243, 156, 18)); // 黄色
                case MessageBoxImage.Information:
                    return Application.Current.Resources["InfoBrush"] as Brush ?? 
                           new SolidColorBrush(Color.FromRgb(52, 152, 219)); // 蓝色
                case MessageBoxImage.Question:
                    return Application.Current.Resources["PrimaryBrush"] as Brush ?? 
                           new SolidColorBrush(Color.FromRgb(52, 73, 94)); // 深灰色
                default:
                    return Application.Current.Resources["PrimaryBrush"] as Brush ?? 
                           new SolidColorBrush(Color.FromRgb(52, 152, 219)); // 默认蓝色
            }
        }

        /// <summary>
        /// 根据消息框图标类型获取图标字符
        /// </summary>
        private static string GetIconSymbol(MessageBoxImage image)
        {
            switch (image)
            {
                case MessageBoxImage.Error:
                    return "×"; // X符号
                case MessageBoxImage.Warning:
                    return "!"; // 感叹号
                case MessageBoxImage.Information:
                    return "i"; // 信息
                case MessageBoxImage.Question:
                    return "?"; // 问号
                default:
                    return "";
            }
        }

        /// <summary>
        /// 创建现代风格按钮
        /// </summary>
        private static Button CreateModernButton(string content, Action clickAction, bool isPrimary)
        {
            // 获取应用资源
            var primaryBrush = Application.Current.Resources["PrimaryBrush"] as SolidColorBrush ?? 
                               new SolidColorBrush(Color.FromRgb(52, 152, 219));
            var primaryDarkBrush = Application.Current.Resources["PrimaryDarkBrush"] as SolidColorBrush ?? 
                                  new SolidColorBrush(Color.FromRgb(41, 128, 185));

            var button = new Button
            {
                Content = content,
                Width = 80,
                Height = 30,
                Margin = new Thickness(2),
                Cursor = System.Windows.Input.Cursors.Hand,
                Padding = new Thickness(10, 5, 10, 5)
            };

            // 主要和次要按钮样式
            if (isPrimary)
            {
                // 创建新的样式
                Style style = new Style(typeof(Button));
                style.Setters.Add(new Setter(Control.BackgroundProperty, primaryBrush));
                style.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush(Colors.White)));
                style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
                
                // 悬停触发器
                var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
                hoverTrigger.Setters.Add(new Setter(Control.BackgroundProperty, primaryDarkBrush));
                style.Triggers.Add(hoverTrigger);
                
                // 应用样式
                button.Style = style;
            }
            else
            {
                // 创建新的样式
                Style style = new Style(typeof(Button));
                style.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Colors.Transparent)));
                style.Setters.Add(new Setter(Control.ForegroundProperty, 
                    Application.Current.Resources["ForegroundBrush"] as Brush ?? new SolidColorBrush(Color.FromRgb(51, 51, 51))));
                style.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
                style.Setters.Add(new Setter(Control.BorderBrushProperty, new SolidColorBrush(Color.FromArgb(50, 0, 0, 0))));
                
                // 悬停触发器
                var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
                hoverTrigger.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromArgb(20, 0, 0, 0))));
                style.Triggers.Add(hoverTrigger);
                
                // 应用样式
                button.Style = style;
            }

            // 点击事件
            button.Click += (sender, e) => clickAction();
            
            return button;
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        public static void ShowError(string message, string title = "错误", Window owner = null)
        {
            Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error, owner);
        }

        /// <summary>
        /// 显示警告消息
        /// </summary>
        public static void ShowWarning(string message, string title = "警告", Window owner = null)
        {
            Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning, owner);
        }

        /// <summary>
        /// 显示信息消息
        /// </summary>
        public static void ShowInfo(string message, string title = "提示", Window owner = null)
        {
            Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information, owner);
        }
    }
}
