using System;
using System.Collections.Generic;
using System.Text;

namespace Heladeria.Contracts.Usuarios
{
    public class UsuarioResponse
    {
        public int UsuarioId { get; set; }

        public string NombreCompleto { get; set; } = string.Empty;

        public string NombreUsuario { get; set; } = string.Empty;

        public string Rol { get; set; } = string.Empty;

        public bool Activo { get; set; }

        public DateTime FechaCreacion { get; set; }
    }
}
