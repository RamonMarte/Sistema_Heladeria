using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Heladeria.Contracts.Ventas
{
    public class CrearVentaRequest
    {
        [Range(1, int.MaxValue)]
        public int CajaId { get; set; }

        [Required]
        [RegularExpression("^(Efectivo|Transferencia)$")]
        public string MetodoPago { get; set; } = string.Empty;

        [Range(
            typeof(decimal),
            "0.01",
            "999999999999.99",
            ParseLimitsInInvariantCulture = true)]
        public decimal? MontoRecibido { get; set; }

        [StringLength(100)]
        public string? Referencia { get; set; }

        [StringLength(500)]
        public string? Observaciones { get; set; }

        [Required]
        [MinLength(1)]
        [MaxLength(100)]
        public List<CrearVentaDetalleRequest> Detalles { get; set; } = [];
    }
}
