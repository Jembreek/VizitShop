using System;
using System.Windows.Input;

namespace VizitShop.Commands
{
    public class RelayCommandImplementation : ICommand
    {
        private readonly Action<object> _executeAction;
        private readonly Func<object, bool> _canExecuteFunc;

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public RelayCommandImplementation(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _executeAction = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecuteFunc = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecuteFunc == null || _canExecuteFunc(parameter);

        public void Execute(object parameter) => _executeAction(parameter);
    }
}