using System;
using System.Collections.Generic;
using System.Text;

namespace Heladeria.Contracts.Auth
{
    public sealed class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;

        public string TokenType { get; set; } = "Bearer";

        public DateTime FechaExpiracionUtc { get; set; }

        public Guid SesionId { get; set; }

        public int UsuarioId { get; set; }

        public string NombreCompleto { get; set; } = string.Empty;

        public string NombreUsuario { get; set; } = string.Empty;

        public string Rol { get; set; } = string.Empty;
    }
}
