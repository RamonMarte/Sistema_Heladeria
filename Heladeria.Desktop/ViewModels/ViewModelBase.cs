using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Heladeria.Desktop.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(
            ref T campo,
            T valor,
            [CallerMemberName] string? propiedad = null)
        {
            if (EqualityComparer<T>.Default.Equals(campo, valor))
                return false;

            campo = valor;
            OnPropertyChanged(propiedad);

            return true;
        }

        protected void OnPropertyChanged(
            [CallerMemberName] string? propiedad = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propiedad));
        }
    }
}
