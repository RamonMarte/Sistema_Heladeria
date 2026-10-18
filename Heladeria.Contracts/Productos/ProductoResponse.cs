using System;
using System.Collections.Generic;
using System.Text;

namespace Heladeria.Contracts.Productos
{
    public class ProductoResponse
    {
        public int ProductoId { get; set; }

        public int CategoriaId { get; set; }

        public string CategoriaNombre { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public string? Sabor { get; set; }

        public string? Descripcion { get; set; }

        public bool Activo { get; set; }

        public DateTime FechaCreacion { get; set; }

        public DateTime? FechaActualizacion { get; set; }

        public List<PresentacionResponse> Presentaciones { get; set; } = [];
    }
}
