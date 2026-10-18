using System;
using System.Collections.Generic;
using System.Text;

namespace Heladeria.Contracts.Ventas
{
    public class PagoResponse
    {
        public long PagoId { get; set; }

        public string MetodoPago { get; set; } = string.Empty;

        public decimal Importe { get; set; }

        public decimal? MontoRecibido { get; set; }

        public decimal Cambio { get; set; }

        public string? Referencia { get; set; }

        public DateTime FechaPago { get; set; }
    }
}
