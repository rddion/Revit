using System;
using System.Windows.Input;

namespace Wpf.ViewModel
{
    class ComandBinding : ICommand
    {
        Action Method { get; set; }
        bool ICommand.CanExecute(object parameter)
        {
            return true;
        }

        event EventHandler ICommand.CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
            }

            remove
            {
                CommandManager.RequerySuggested -= value;
            }
        }

        void ICommand.Execute(object parameter)
        {
            Method?.Invoke();
        }

        public ComandBinding(Action action)
        {
            Method = action;
        }


    }
}
