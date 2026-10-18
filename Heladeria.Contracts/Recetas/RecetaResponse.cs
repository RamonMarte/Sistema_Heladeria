using System;
using System.Collections.Generic;
using System.Text;

namespace Heladeria.Contracts.Recetas
{
    public class RecetaResponse
    {
        public int ProductoPresentacionId { get; set; }

        public string Producto { get; set; } = string.Empty;

        public string Presentacion { get; set; } = string.Empty;

        public List<RecetaIngredienteResponse> Ingredientes { get; set; } = [];
    }

    public class RecetaIngredienteResponse
    {
        public int ArticuloInventarioId { get; set; }

        public string NombreArticulo { get; set; } = string.Empty;

        public string TipoArticulo { get; set; } = string.Empty;

        public string UnidadMedida { get; set; } = string.Empty;

        public string Abreviatura { get; set; } = string.Empty;

        public decimal CantidadPorUnidad { get; set; }
    }
}
