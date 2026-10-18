namespace Heladeria.Api.Entities
{
    public class DetalleVenta
    {
        public long DetalleVentaId { get; set; }

        public long VentaId { get; set; }

        public int ProductoPresentacionId { get; set; }

        public string NombreProducto { get; set; } = string.Empty;

        public string NombrePresentacion { get; set; } = string.Empty;

        public string CodigoInterno { get; set; } = string.Empty;

        public decimal Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        public decimal CostoUnitario { get; set; }

        public decimal Descuento { get; set; }

        public decimal ImporteLinea { get; set; }

        public Venta Venta { get; set; } = null!;

        public ProductoPresentacion ProductoPresentacion { get; set; }
            = null!;
    }
}
