using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Heladeria.Contracts.Usuarios
{
    public class CambiarContrasenaUsuarioRequest
    {
        [Required]
        [StringLength(128, MinimumLength = 12)]
        public string NuevaContrasena { get; set; } = string.Empty;
    }
}
