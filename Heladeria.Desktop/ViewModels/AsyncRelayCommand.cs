using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace Heladeria.Desktop.ViewModels
{
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _ejecutar;
        private readonly Func<bool>? _puedeEjecutar;

        public bool EstaEjecutando { get; private set; }

        public event EventHandler? CanExecuteChanged;

        public AsyncRelayCommand(
            Func<Task> ejecutar,
            Func<bool>? puedeEjecutar = null)
        {
            _ejecutar = ejecutar;
            _puedeEjecutar = puedeEjecutar;
        }

        public bool CanExecute(object? parameter)
        {
            return !EstaEjecutando &&
                   (_puedeEjecutar?.Invoke() ?? true);
        }

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
                return;

            EstaEjecutando = true;
            NotificarCambio();

            try
            {
                await _ejecutar();
            }
            finally
            {
                EstaEjecutando = false;
                NotificarCambio();
            }
        }

        public void NotificarCambio()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
