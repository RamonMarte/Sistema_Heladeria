using Heladeria.Contracts.Compras;
using System;
using System.Collections.Generic;
using System.Text;

namespace Heladeria.Desktop.ViewModels
{
    public class RecepcionLineaViewModel : ViewModelBase
    {
        private string _codigoLote = string.Empty;
        private DateTime? _fechaVencimiento;

        public long CompraDetalleId { get; set; }

        public string NombreArticulo { get; set; } = string.Empty;

        public decimal Cantidad { get; set; }

        public string Abreviatura { get; set; } = string.Empty;

        public string CodigoLote
        {
            get => _codigoLote;
            set => SetProperty(ref _codigoLote, value);
        }

        public DateTime? FechaVencimiento
        {
            get => _fechaVencimiento;
            set => SetProperty(ref _fechaVencimiento, value);
        }

        public RecepcionLineaViewModel(CompraDetalleResponse detalle)
        {
            CompraDetalleId = detalle.CompraDetalleId;
            NombreArticulo = detalle.NombreArticulo;
            Cantidad = detalle.Cantidad;
            Abreviatura = detalle.Abreviatura;
        }
    }
}
