using System;
using System.Collections.Generic;
using System.Text;

namespace Heladeria.Contracts.Inventario
{
    public class ArticuloInventarioResponse
    {
        public int ArticuloInventarioId { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string TipoArticulo { get; set; } = string.Empty;

        public int UnidadMedidaId { get; set; }

        public string UnidadMedida { get; set; } = string.Empty;

        public string Abreviatura { get; set; } = string.Empty;

        public int? ProductoPresentacionId { get; set; }

        public decimal StockMinimo { get; set; }

        public bool Activo { get; set; }

        public DateTime FechaCreacion { get; set; }
    }
}
