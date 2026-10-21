using System;
using System.Collections.Generic;
using System.Text;

namespace Heladeria.Contracts.Sesiones
{
    public class SesionHistorialResponse
    {
        public Guid SesionId { get; set; }

        public int UsuarioId { get; set; }

        public string NombreCompleto { get; set; } = string.Empty;

        public string NombreUsuario { get; set; } = string.Empty;

        public string Rol { get; set; } = string.Empty;

        public DateTime FechaHoraInicio { get; set; }

        public DateTime? FechaHoraCierre { get; set; }

        public decimal DuracionHoras { get; set; }

        public string Estado { get; set; } = string.Empty;

        public string? MotivoCierre { get; set; }

        public string? Equipo { get; set; }

        public string? DireccionIP { get; set; }
    }
}
