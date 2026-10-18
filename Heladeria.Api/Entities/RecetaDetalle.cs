namespace Heladeria.Api.Entities
{
    public class RecetaDetalle
    {
        public long RecetaDetalleId { get; set; }

        public int ProductoPresentacionId { get; set; }

        public int ArticuloInventarioId { get; set; }

        public decimal CantidadPorUnidad { get; set; }

        public ProductoPresentacion ProductoPresentacion { get; set; }
            = null!;

        public ArticuloInventario ArticuloInventario { get; set; }
            = null!;
    }
}
