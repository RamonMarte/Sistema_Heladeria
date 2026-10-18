using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Heladeria.Contracts.Auth
{
    public sealed class LoginRequest
    {
        [Required]
        [StringLength(60, MinimumLength = 1)]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required]
        public string Contrasena { get; set; } = string.Empty;

        [StringLength(128)]
        public string? Equipo { get; set; }
    }
}
