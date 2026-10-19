using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Heladeria.Desktop.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _ejecutar;
        private readonly Predicate<object?>? _puedeEjecutar;

        public event EventHandler? CanExecuteChanged;

        public RelayCommand(
            Action<object?> ejecutar,
            Predicate<object?>? puedeEjecutar = null)
        {
            _ejecutar = ejecutar;
            _puedeEjecutar = puedeEjecutar;
        }

        public bool CanExecute(object? parameter)
        {
            return _puedeEjecutar?.Invoke(parameter) ?? true;
        }

        public void Execute(object? parameter)
        {
            _ejecutar(parameter);
        }

        public void NotificarCambio()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
