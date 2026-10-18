namespace Heladeria.Api.Entities
{
    public class CompraDetalle
    {
        public long CompraDetalleId { get; set; }

        public long CompraId { get; set; }

        public int ArticuloInventarioId { get; set; }

        public decimal Cantidad { get; set; }

        public decimal CostoUnitario { get; set; }

        public decimal Descuento { get; set; }

        public decimal Importe { get; set; }

        public Compra Compra { get; set; } = null!;

        public ArticuloInventario ArticuloInventario { get; set; } = null!;
    }
}
