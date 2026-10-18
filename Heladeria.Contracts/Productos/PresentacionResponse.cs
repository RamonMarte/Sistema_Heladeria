using System;
using System.Collections.Generic;
using System.Text;

namespace Heladeria.Contracts.Productos
{
    public class PresentacionResponse
    {
        public int ProductoPresentacionId { get; set; }

        public string NombrePresentacion { get; set; } = string.Empty;

        public string? Tamano { get; set; }

        public string CodigoInterno { get; set; } = string.Empty;

        public string? CodigoBarras { get; set; }

        public decimal PrecioVenta { get; set; }

        public decimal CostoEstimado { get; set; }

        public bool Activa { get; set; }

        public DateTime FechaCreacion { get; set; }
    }
}
