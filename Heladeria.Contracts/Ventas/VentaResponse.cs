using System;
using System.Collections.Generic;
using System.Text;

namespace Heladeria.Contracts.Ventas
{
    public class VentaResponse
    {
        public long VentaId { get; set; }

        public string NumeroVenta { get; set; } = string.Empty;

        public DateTime FechaVenta { get; set; }

        public int UsuarioId { get; set; }

        public string NombreUsuario { get; set; } = string.Empty;

        public int CajaId { get; set; }

        public string NombreCaja { get; set; } = string.Empty;

        public string Estado { get; set; } = string.Empty;

        public decimal Subtotal { get; set; }

        public decimal Descuento { get; set; }

        public decimal Total { get; set; }

        public string? Observaciones { get; set; }

        public List<VentaDetalleResponse> Detalles { get; set; } = [];

        public List<PagoResponse> Pagos { get; set; } = [];
    }
}
