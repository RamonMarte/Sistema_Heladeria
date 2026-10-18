using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Heladeria.Contracts.Compras
{
    public class CrearCompraDetalleRequest
    {
        [Range(1, int.MaxValue)]
        public int ArticuloInventarioId { get; set; }

        [Range(
            typeof(decimal),
            "0.001",
            "99999999999.999",
            ParseLimitsInInvariantCulture = true)]
        public decimal Cantidad { get; set; }

        [Range(
            typeof(decimal),
            "0",
            "99999999.9999",
            ParseLimitsInInvariantCulture = true)]
        public decimal CostoUnitario { get; set; }

        [Range(
            typeof(decimal),
            "0",
            "999999999999.99",
            ParseLimitsInInvariantCulture = true)]
        public decimal Descuento { get; set; }
    }
}
