using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Heladeria.Contracts.Compras
{
    public class CrearCompraRequest
    {
        [Required]
        [StringLength(40, MinimumLength = 1)]
        public string NumeroCompra { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int ProveedorId { get; set; }

        [Range(
            typeof(decimal),
            "0",
            "999999999999.99",
            ParseLimitsInInvariantCulture = true)]
        public decimal Descuento { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        [Required]
        [MinLength(1)]
        public List<CrearCompraDetalleRequest> Detalles { get; set; } = [];
    }
}
