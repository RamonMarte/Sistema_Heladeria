namespace Heladeria.Api.Entities
{
    public class Producto
    {
        public int ProductoId { get; set; }

        public int CategoriaId { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string? Sabor { get; set; }

        public string? Descripcion { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; }

        public DateTime? FechaActualizacion { get; set; }

        public Categoria Categoria { get; set; } = null!;

        public ICollection<ProductoPresentacion> Presentaciones { get; set; }
            = new List<ProductoPresentacion>();
    }
}
