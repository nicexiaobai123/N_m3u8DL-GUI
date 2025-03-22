using System;
using System.Windows.Input;

namespace N_m3u8DL_CLI.GUI.Helpers
{
    /// <summary>
    /// 实现ICommand接口的简单命令类
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        /// <summary>
        /// 创建一个始终可执行的命令
        /// </summary>
        /// <param name="execute">执行函数</param>
        public RelayCommand(Action<object> execute) : this(execute, null) { }

        /// <summary>
        /// 创建一个命令
        /// </summary>
        /// <param name="execute">执行函数</param>
        /// <param name="canExecute">判断是否可以执行的函数</param>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// 判断命令是否可以执行
        /// </summary>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        /// <summary>
        /// 执行命令
        /// </summary>
        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        /// <summary>
        /// 可执行状态改变时触发
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// 手动触发CanExecuteChanged事件
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
