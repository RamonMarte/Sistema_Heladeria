namespace Heladeria.Api.Entities
{
    public class ProductoPresentacion
    {
        public int ProductoPresentacionId { get; set; }

        public int ProductoId { get; set; }

        public string NombrePresentacion { get; set; } = string.Empty;

        public string? Tamano { get; set; }

        public string CodigoInterno { get; set; } = string.Empty;

        public string? CodigoBarras { get; set; }

        public decimal PrecioVenta { get; set; }

        public decimal CostoEstimado { get; set; }

        public bool Activa { get; set; } = true;

        public DateTime FechaCreacion { get; set; }

        public Producto Producto { get; set; } = null!;
    }
}
