namespace Heladeria.Api.Entities
{
    public class Usuario
    {
        public int UsuarioId { get; set; }

        public string NombreCompleto { get; set; } = string.Empty;

        public string NombreUsuario { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Rol { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

        public DateTime FechaCreacion { get; set; }

        public ICollection<SesionUsuario> Sesiones { get; set; }
            = new List<SesionUsuario>();
    }
}
