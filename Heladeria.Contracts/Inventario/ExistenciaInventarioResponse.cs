using System;
using System.Collections.Generic;
using System.Text;

namespace Heladeria.Contracts.Inventario
{
    public class ExistenciaInventarioResponse
    {
        public int ArticuloInventarioId { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string TipoArticulo { get; set; } = string.Empty;

        public string UnidadMedida { get; set; } = string.Empty;

        public string Abreviatura { get; set; } = string.Empty;

        public decimal StockMinimo { get; set; }

        public decimal ExistenciaActual { get; set; }

        public bool RequiereReposicion { get; set; }
    }
}
