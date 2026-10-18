using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Heladeria.Contracts.Inventario
{
    public class RegistrarMermaRequest
    {
        [Range(1, int.MaxValue)]
        public int ArticuloInventarioId { get; set; }

        [Range(typeof(decimal),"0.001","99999999999.999",ParseLimitsInInvariantCulture = true)]
        public decimal Cantidad { get; set; }

        public long? LoteInventarioId { get; set; }

        [Required]
        [RegularExpression(
            "^(Vencido>|Dañado|Danado|Derrame|Contaminado|ConsumoInterno|Otro)$")]
        public string Motivo { get; set; } = string.Empty;

        [StringLength(240)]
        public string? Observaciones { get; set; }
    }
}
