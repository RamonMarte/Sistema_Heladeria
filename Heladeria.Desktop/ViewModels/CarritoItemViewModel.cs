using Heladeria.Contracts.Productos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Heladeria.Desktop.ViewModels
{
    public class CarritoItemViewModel : ViewModelBase
    {
        private decimal _cantidad = 1;

        public CatalogoVentaResponse Producto { get; }

        public decimal Cantidad
        {
            get => _cantidad;
            set
            {
                if (value <= 0 || value > 999999999.999m)
                    return;

                if (SetProperty(ref _cantidad, value))
                    OnPropertyChanged(nameof(Importe));
            }
        }

        public decimal PrecioUnitario => Producto.PrecioVenta;

        public decimal Importe =>
            Math.Round(
                Cantidad * PrecioUnitario,
                2,
                MidpointRounding.AwayFromZero);

        public CarritoItemViewModel(CatalogoVentaResponse producto)
        {
            Producto = producto;
        }
    }
}
