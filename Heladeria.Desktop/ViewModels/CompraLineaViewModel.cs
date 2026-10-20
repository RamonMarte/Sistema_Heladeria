using Heladeria.Contracts.Inventario;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Heladeria.Desktop.ViewModels
{
    public class CompraLineaViewModel : ViewModelBase
    {
        private ArticuloInventarioResponse? _articuloSeleccionado;
        private string _cantidadTexto = "1";
        private string _costoUnitarioTexto = "0.00";
        private string _descuentoTexto = "0.00";

        public event Action? Modificado;

        public ArticuloInventarioResponse? ArticuloSeleccionado
        {
            get => _articuloSeleccionado;
            set
            {
                if (SetProperty(ref _articuloSeleccionado, value))
                    NotificarCambio();
            }
        }

        public string CantidadTexto
        {
            get => _cantidadTexto;
            set
            {
                if (SetProperty(ref _cantidadTexto, value))
                    NotificarCambio();
            }
        }

        public string CostoUnitarioTexto
        {
            get => _costoUnitarioTexto;
            set
            {
                if (SetProperty(ref _costoUnitarioTexto, value))
                    NotificarCambio();
            }
        }

        public string DescuentoTexto
        {
            get => _descuentoTexto;
            set
            {
                if (SetProperty(ref _descuentoTexto, value))
                    NotificarCambio();
            }
        }

        public decimal Importe
        {
            get
            {
                if (!DecimalValido(CantidadTexto, out var cantidad) ||
                    !DecimalValido(CostoUnitarioTexto, out var costo) ||
                    !DecimalValido(DescuentoTexto, out var descuento))
                    return 0;

                return Math.Round(
                    cantidad * costo,
                    2,
                    MidpointRounding.AwayFromZero) - descuento;
            }
        }

        private void NotificarCambio()
        {
            OnPropertyChanged(nameof(Importe));
            Modificado?.Invoke();
        }

        private static bool DecimalValido(string texto, out decimal valor)
        {
            return decimal.TryParse(
                       texto,
                       NumberStyles.Number,
                       CultureInfo.CurrentCulture,
                       out valor)
                   || decimal.TryParse(
                       texto,
                       NumberStyles.Number,
                       CultureInfo.InvariantCulture,
                       out valor);
        }
    }
}
