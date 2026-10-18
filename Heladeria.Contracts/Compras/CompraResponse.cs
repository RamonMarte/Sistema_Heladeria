using System;
using System.Collections.Generic;
using System.Text;

namespace Heladeria.Contracts.Compras
{
    public class CompraResponse
    {
        public long CompraId { get; set; }

        public string NumeroCompra { get; set; } = string.Empty;

        public int ProveedorId { get; set; }

        public string ProveedorNombre { get; set; } = string.Empty;

        public int UsuarioId { get; set; }

        public DateTime FechaCompra { get; set; }

        public string Estado { get; set; } = string.Empty;

        public decimal Subtotal { get; set; }

        public decimal Descuento { get; set; }

        public decimal Total { get; set; }

        public string? Observaciones { get; set; }

        public List<CompraDetalleResponse> Detalles { get; set; } = [];
    }
}
