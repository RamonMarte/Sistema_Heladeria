using System;
using System.Collections.Generic;
using System.Text;

namespace Heladeria.Contracts.Proveedores
{
    public class ProveedorResponse
    {
        public int ProveedorId { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string? RNC { get; set; }

        public string? Telefono { get; set; }

        public string? Correo { get; set; }

        public string? Direccion { get; set; }

        public bool Activo { get; set; }

        public DateTime FechaCreacion { get; set; }
    }
}
