using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ThreeNH.Helper.Plugin
{
    public  class RelayCommand : ICommand
    {

        readonly Action<object> _execute;
        readonly Predicate<object> _canexecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute;
            _canexecute = canExecute;
        }

        public bool CanExecute(object para)
        {
            return _canexecute == null ? true : _canexecute(para);
        }
        public void Execute(object para)
        {
            _execute(para);
        }
       
    }
}
