using System;
using System.Collections.Generic;
using System.Text;

namespace Heladeria.Contracts.Inventario
{
    public class UnidadMedidaResponse
    {
        public int UnidadMedidaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Abreviatura { get; set; } = string.Empty;
    }
}
