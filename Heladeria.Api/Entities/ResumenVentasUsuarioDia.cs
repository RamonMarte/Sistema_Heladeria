namespace Heladeria.Api.Entities
{
    public class ResumenVentasUsuarioDia
    {
        public int UsuarioId { get; set; }

        public string NombreCompleto { get; set; } = string.Empty;

        public string NombreUsuario { get; set; } = string.Empty;

        public string Rol { get; set; } = string.Empty;

        public DateTime Fecha { get; set; }

        public long CantidadVentas { get; set; }

        public decimal CantidadHeladosVendidos { get; set; }

        public decimal DineroGenerado { get; set; }

        public decimal GananciaBrutaEstimada { get; set; }
    }
}
