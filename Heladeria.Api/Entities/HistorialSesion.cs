namespace Heladeria.Api.Entities
{
    public class HistorialSesion
    {
        public Guid SesionId { get; set; }

        public int UsuarioId { get; set; }

        public string NombreCompleto { get; set; } = string.Empty;

        public string NombreUsuario { get; set; } = string.Empty;

        public string Rol { get; set; } = string.Empty;

        public DateTime FechaHoraInicio { get; set; }

        public DateTime? FechaHoraCierre { get; set; }

        public string EstadoSesion { get; set; } = string.Empty;

        public string? MotivoCierre { get; set; }

        public int DuracionMinutos { get; set; }

        public string? Equipo { get; set; }

        public string? DireccionIP { get; set; }
    }
}
