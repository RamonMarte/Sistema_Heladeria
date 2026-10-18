namespace Heladeria.Api.Entities
{
    public class Compra
    {
        public long CompraId { get; set; }

        public string NumeroCompra { get; set; } = string.Empty;

        public int ProveedorId { get; set; }

        public int UsuarioId { get; set; }

        public DateTime FechaCompra { get; set; }

        public string Estado { get; set; } = "Borrador";

        public decimal Subtotal { get; set; }

        public decimal Descuento { get; set; }

        public decimal Total { get; set; }

        public string? Observaciones { get; set; }

        public Proveedor Proveedor { get; set; } = null!;

        public ICollection<CompraDetalle> Detalles { get; set; }
            = new List<CompraDetalle>();
    }
}
