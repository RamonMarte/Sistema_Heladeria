using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Heladeria.Contracts.Usuarios
{
    public class ActualizarUsuarioRequest
    {
        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required]
        [RegularExpression("^(Encargado|Cajero)$")]
        public string Rol { get; set; } = "Cajero";

        public bool Activo { get; set; } = true;
    }
}
