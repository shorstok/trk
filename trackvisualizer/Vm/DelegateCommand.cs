using System;
using System.Windows.Input;

namespace trackvisualizer.Vm
{
    public class DelegateCommand : ICommand
    {
        readonly Func<object, bool> _canExecute;
        readonly Action<object> _execute;

        //Конструктор
        public DelegateCommand(Func<object, bool> canExecute, Action<object> execute)
        {
            this._canExecute = canExecute;
            this._execute = execute;
        }

        //Проверка доступности команды
        public bool CanExecute(object parameter)
        {
            return _canExecute(parameter);
        }

        //Выполнение команды
        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        
        //Служебное событие
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested += value;
        }
    }
}
