namespace Heladeria.Api.Entities
{
    public class MovimientoInventario
    {
        public long MovimientoInventarioId { get; set; }

        public int ArticuloInventarioId { get; set; }

        public long LoteInventarioId { get; set; }

        public string TipoMovimiento { get; set; } = string.Empty;

        public string Direccion { get; set; } = string.Empty;

        public decimal Cantidad { get; set; }

        public decimal CostoUnitario { get; set; }

        public DateTime FechaMovimiento { get; set; }

        public int? UsuarioId { get; set; }

        public string? ReferenciaTipo { get; set; }

        public long? ReferenciaId { get; set; }

        public string? Observaciones { get; set; }
    }
}
