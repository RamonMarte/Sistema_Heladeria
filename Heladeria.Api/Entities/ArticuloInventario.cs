namespace Heladeria.Api.Entities
{
    public class ArticuloInventario
    {
        public int ArticuloInventarioId { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string TipoArticulo { get; set; } = string.Empty;

        public int UnidadMedidaId { get; set; }

        public int? ProductoPresentacionId { get; set; }

        public decimal StockMinimo { get; set; }

        public bool Activo { get; set; }

        public DateTime FechaCreacion { get; set; }

        public UnidadMedida UnidadMedida { get; set; } = null!;

        public ProductoPresentacion? ProductoPresentacion { get; set; }
    }
}
