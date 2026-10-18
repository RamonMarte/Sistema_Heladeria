using System;
using System.Collections.Generic;
using System.Text;

namespace Heladeria.Contracts.Reportes
{
    public class VentasEmpleadoReporteResponse
    {
        public int UsuarioId { get; set; }

        public string NombreUsuario { get; set; } = string.Empty;

        public string NombreCompleto { get; set; } = string.Empty;

        public long CantidadTransacciones { get; set; }

        public decimal ImporteTotalTickets { get; set; }

        public decimal UnidadesVendidas { get; set; }

        public decimal ImporteProductos { get; set; }

        public decimal CostoEstimadoProductos { get; set; }

        public decimal GananciaBrutaEstimada { get; set; }
    }
}
