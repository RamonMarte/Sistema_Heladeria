using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Heladeria.Contracts.Inventario
{
    public class RegistrarAjusteInventarioRequest
    {
        [Range(1, int.MaxValue)]
        public int ArticuloInventarioId { get; set; }

        [Range(
            typeof(decimal),
            "0",
            "99999999999.999",
            ParseLimitsInInvariantCulture = true)]
        public decimal CantidadContada { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string Motivo { get; set; } = string.Empty;

        [Range(
            typeof(decimal),
            "0",
            "99999999.9999",
            ParseLimitsInInvariantCulture = true)]
        public decimal? CostoUnitarioEntrada { get; set; }

        [StringLength(80)]
        public string? CodigoLote { get; set; }

        public DateOnly? FechaVencimiento { get; set; }

        [StringLength(240)]
        public string? Observaciones { get; set; }
    }
}
