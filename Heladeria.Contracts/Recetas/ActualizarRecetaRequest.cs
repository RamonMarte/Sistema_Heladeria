using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Heladeria.Contracts.Recetas
{
    public class ActualizarRecetaRequest
    {
        [Required]
        [MinLength(1)]
        [MaxLength(50)]
        public List<RecetaIngredienteRequest> Ingredientes { get; set; } = [];
    }

    public class RecetaIngredienteRequest
    {
        [Range(1, int.MaxValue)]
        public int ArticuloInventarioId { get; set; }

        [Range(
            typeof(decimal),
            "0.0001",
            "9999999999.9999",
            ParseLimitsInInvariantCulture = true)]
        public decimal CantidadPorUnidad { get; set; }
    }
}
