using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Heladeria.Contracts.Productos
{
    public class ActualizarPresentacionRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string NombrePresentacion { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Tamano { get; set; }

        [Required]
        [StringLength(40, MinimumLength = 1)]
        public string CodigoInterno { get; set; } = string.Empty;

        [StringLength(80)]
        public string? CodigoBarras { get; set; }

        [Range(typeof(decimal),"0","9999999999.99",ParseLimitsInInvariantCulture = true)]
        public decimal PrecioVenta { get; set; }

        [Range(typeof(decimal),"0","9999999999.99",ParseLimitsInInvariantCulture = true)]
        public decimal CostoEstimado { get; set; }

        public bool Activa { get; set; } = true;
    }
}
