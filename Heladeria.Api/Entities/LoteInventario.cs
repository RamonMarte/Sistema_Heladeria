namespace Heladeria.Api.Entities
{
    public class LoteInventario
    {
        public long LoteInventarioId { get; set; }

        public int ArticuloInventarioId { get; set; }

        public long? CompraDetalleId { get; set; }

        public string? CodigoLote { get; set; }

        public DateOnly? FechaVencimiento { get; set; }

        public DateTime FechaIngreso { get; set; }

        public decimal CantidadInicial { get; set; }

        public decimal CantidadActual { get; set; }

        public decimal CostoUnitario { get; set; }

        public bool Activo { get; set; } = true;
    }
}
