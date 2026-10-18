using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Heladeria.Contracts.Proveedores
{
    public class ProveedorRequest
    {
        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(20)]
        public string? RNC { get; set; }

        [StringLength(30)]
        public string? Telefono { get; set; }

        [EmailAddress]
        [StringLength(150)]
        public string? Correo { get; set; }

        [StringLength(250)]
        public string? Direccion { get; set; }

        public bool Activo { get; set; } = true;
    }
}
