using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Heladeria.Contracts.Usuarios
{
    public class CrearUsuarioRequest
    {
        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required]
        [StringLength(60, MinimumLength = 3)]
        [RegularExpression(
            @"^[a-zA-Z0-9._-]+$",
            ErrorMessage = "Usa letras, números, puntos, guiones o guiones bajos.")]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required]
        [StringLength(128, MinimumLength = 12)]
        public string Contrasena { get; set; } = string.Empty;

        [Required]
        [RegularExpression(
            "^(Encargado|Cajero)$",
            ErrorMessage = "El rol debe ser Encargado o Cajero.")]
        public string Rol { get; set; } = "Cajero";
    }
}
