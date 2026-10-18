using System;
using System.Collections.Generic;
using System.Text;

namespace Heladeria.Contracts.Cajas
{
    public class CajaResponse
    {
        public int CajaId { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public bool Activa { get; set; }
    }
}
