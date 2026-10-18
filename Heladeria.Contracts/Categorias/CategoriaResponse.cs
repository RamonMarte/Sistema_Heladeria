using System;
using System.Collections.Generic;
using System.Text;

namespace Heladeria.Contracts.Categorias
{
    public class CategoriaResponse
    {
        public int CategoriaId { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public bool Activa { get; set; }

        public DateTime FechaCreacion { get; set; }
    }
}
