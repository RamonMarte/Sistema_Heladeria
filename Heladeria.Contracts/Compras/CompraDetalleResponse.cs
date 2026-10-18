using System;
using System.Collections.Generic;
using System.Text;

namespace Heladeria.Contracts.Compras
{
    public class CompraDetalleResponse
    {
        public long CompraDetalleId { get; set; }

        public int ArticuloInventarioId { get; set; }

        public string NombreArticulo { get; set; } = string.Empty;

        public string UnidadMedida { get; set; } = string.Empty;

        public string Abreviatura { get; set; } = string.Empty;

        public decimal Cantidad { get; set; }

        public decimal CostoUnitario { get; set; }

        public decimal Descuento { get; set; }

        public decimal Importe { get; set; }
    }
}
