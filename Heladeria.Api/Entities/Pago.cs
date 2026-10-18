namespace Heladeria.Api.Entities
{
    public class Pago
    {
        public long PagoId { get; set; }

        public long VentaId { get; set; }

        public string MetodoPago { get; set; } = string.Empty;

        public decimal Importe { get; set; }

        public string? Referencia { get; set; }

        public DateTime FechaPago { get; set; }

        public decimal? MontoRecibido { get; set; }

        public decimal Cambio { get; set; }

        public Venta Venta { get; set; } = null!;
    }
}
