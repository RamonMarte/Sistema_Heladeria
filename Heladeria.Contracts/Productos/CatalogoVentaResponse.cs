using System;
using System.Collections.Generic;
using System.Text;

namespace Heladeria.Contracts.Productos
{
    public class CatalogoVentaResponse
    {
        public int ProductoPresentacionId { get; set; }

        public int ProductoId { get; set; }

        public string NombreProducto { get; set; } = string.Empty;

        public string? Sabor { get; set; }

        public string Categoria { get; set; } = string.Empty;

        public string NombrePresentacion { get; set; } = string.Empty;

        public string? Tamano { get; set; }

        public string CodigoInterno { get; set; } = string.Empty;

        public string? CodigoBarras { get; set; }

        public decimal PrecioVenta { get; set; }
    }
}
