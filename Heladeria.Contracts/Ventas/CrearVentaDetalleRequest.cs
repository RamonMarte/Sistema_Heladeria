using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Heladeria.Contracts.Ventas
{
    public class CrearVentaDetalleRequest
    {
        [Range(1, int.MaxValue)]
        public int ProductoPresentacionId { get; set; }

        [Range(
            typeof(decimal),
            "0.001",
            "999999999.999",
            ParseLimitsInInvariantCulture = true)]
        public decimal Cantidad { get; set; }
    }
}
