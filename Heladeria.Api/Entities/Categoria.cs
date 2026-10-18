namespace Heladeria.Api.Entities
{
    public class Categoria
    {
        public int CategoriaId { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string? Descripcion { get; set; }

        public bool Activa { get; set; } = true;

        public DateTime FechaCreacion { get; set; }

        public ICollection<Producto> Productos { get; set; }
            = new List<Producto>();
    }
}
