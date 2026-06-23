using System;
using System.Windows.Input;

namespace MyFlowChart.Models
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        /// <summary>
        /// 初始化通用命令。
        /// </summary>
        /// <param name="execute">命令执行逻辑。</param>
        /// <param name="canExecute">命令可执行判断。</param>
        /// <returns>无返回值。</returns>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// 判断当前命令是否可以执行。
        /// </summary>
        /// <param name="parameter">命令参数。</param>
        /// <returns>可执行时返回 true，否则返回 false。</returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        /// <summary>
        /// 执行当前命令。
        /// </summary>
        /// <param name="parameter">命令参数。</param>
        /// <returns>无返回值。</returns>
        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }
}
