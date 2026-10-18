using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Heladeria.Contracts.Cajas
{
    public class CrearCajaRequest
    {
        [Required]
        [StringLength(30, MinimumLength = 2)]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        [StringLength(80, MinimumLength = 2)]
        public string Nombre { get; set; } = string.Empty;
    }
}
