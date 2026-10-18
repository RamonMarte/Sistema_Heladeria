namespace Heladeria.Api.Entities
{
    public class ExistenciaInventario
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
