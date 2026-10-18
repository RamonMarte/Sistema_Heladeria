using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Heladeria.Contracts.Inventario
{
    public class CrearArticuloInventarioRequest
    {
        [StringLength(150, MinimumLength = 2)]
        public string? Nombre { get; set; }

        [Required]
        [RegularExpression("^(Ingrediente|Material|ProductoTerminado)$",
            ErrorMessage = "El tipo de artículo no es válido.")]
        public string TipoArticulo { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int UnidadMedidaId { get; set; }

        public int? ProductoPresentacionId { get; set; }

        [Range(typeof(decimal),"0","99999999999.999",ParseLimitsInInvariantCulture = true)]
        public decimal StockMinimo { get; set; }
    }
}
