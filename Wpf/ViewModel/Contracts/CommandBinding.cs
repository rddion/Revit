using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Wpf.ViewModel.Contracts
{
    internal class CommandBinding : ICommand
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

        public CommandBinding(Action action)
        {
            Method = action;
        }
    }
}
