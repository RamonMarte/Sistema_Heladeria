namespace Heladeria.Api.Entities
{
    public class Proveedor
    {
        public int ProveedorId { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string? RNC { get; set; }

        public string? Telefono { get; set; }

        public string? Correo { get; set; }

        public string? Direccion { get; set; }

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; }
    }
}
