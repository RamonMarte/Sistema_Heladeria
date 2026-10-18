namespace Heladeria.Api.Entities
{
    public class SesionUsuario
    {
        public Guid SesionId { get; set; }

        public int UsuarioId { get; set; }

        public DateTime FechaHoraInicio { get; set; }

        public DateTime? FechaHoraCierre { get; set; }

        public string? MotivoCierre { get; set; }

        public string? Equipo { get; set; }

        public string? DireccionIP { get; set; }

        public Usuario Usuario { get; set; } = null!;
    }
}
