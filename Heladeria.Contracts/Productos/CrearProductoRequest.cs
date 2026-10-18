using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Heladeria.Contracts.Productos
{
    public class CrearProductoRequest
    {
        [Range(1, int.MaxValue)]
        public int CategoriaId { get; set; }

        [Required]
        [StringLength(120, MinimumLength = 2)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(80)]
        public string? Sabor { get; set; }

        [StringLength(300)]
        public string? Descripcion { get; set; }
    }
}
