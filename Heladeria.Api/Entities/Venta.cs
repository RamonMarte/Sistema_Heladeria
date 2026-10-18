namespace Heladeria.Api.Entities
{
    public class Venta
    {
        public long VentaId { get; set; }

        public string NumeroVenta { get; set; } = string.Empty;

        public DateTime FechaVenta { get; set; }

        public int UsuarioId { get; set; }

        public int CajaId { get; set; }

        public Usuario Usuario { get; set; } = null!;

        public Caja Caja { get; set; } = null!;

        public string Estado { get; set; } = "Completada";

        public decimal Subtotal { get; set; }

        public decimal Descuento { get; set; }

        public decimal Total { get; set; }

        public string? Observaciones { get; set; }

        public ICollection<DetalleVenta> Detalles { get; set; }
            = new List<DetalleVenta>();

        public ICollection<Pago> Pagos { get; set; }
            = new List<Pago>();
    }
}
