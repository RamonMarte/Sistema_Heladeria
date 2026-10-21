using Heladeria.Api.Data;
using Heladeria.Contracts.Sesiones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Api.Controllers
{
    [ApiController]
    [Route("api/admin/historial-sesiones")]
    [Authorize(Roles = "Administrador")]
    public class HistorialSesionesController : ControllerBase
    {
        private readonly HeladeriaDbContext _context;

        public HistorialSesionesController(
            HeladeriaDbContext context)
        {
            _context = context;
        }

        // GET: api/admin/sesiones
        [HttpGet]
        public async Task<IActionResult> ObtenerHistorial(
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta,
            [FromQuery] int? usuarioId,
            [FromQuery] bool soloAbiertas = false)
        {
            if (desde.HasValue &&
                hasta.HasValue &&
                desde.Value.Date > hasta.Value.Date)
            {
                return BadRequest(new
                {
                    mensaje =
                        "La fecha inicial no puede superar la fecha final."
                });
            }

            var consulta = _context.SesionesUsuario
                .AsNoTracking()
                .Include(s => s.Usuario)
                .AsQueryable();

            // Filtrar desde una fecha.
            if (desde.HasValue)
            {
                var fechaInicio = desde.Value.Date;

                consulta = consulta.Where(s =>
                    s.FechaHoraInicio >= fechaInicio);
            }

            // Incluir todo el día indicado en la fecha final.
            if (hasta.HasValue)
            {
                var fechaFinExclusiva = hasta.Value.Date.AddDays(1);

                consulta = consulta.Where(s =>
                    s.FechaHoraInicio < fechaFinExclusiva);
            }

            // Filtrar por empleado.
            if (usuarioId.HasValue)
            {
                if (usuarioId.Value <= 0)
                {
                    return BadRequest(new
                    {
                        mensaje = "El identificador del empleado no es válido."
                    });
                }

                consulta = consulta.Where(s =>
                    s.UsuarioId == usuarioId.Value);
            }

            // Mostrar solamente las sesiones que no tienen cierre.
            if (soloAbiertas)
            {
                consulta = consulta.Where(s =>
                    s.FechaHoraCierre == null);
            }

            var sesiones = await consulta
                .OrderByDescending(s => s.FechaHoraInicio)
                .Take(1000)
                .ToListAsync();

            var ahora = DateTime.Now;

            var resultado = sesiones.Select(s =>
            {
                var fechaFin =
                    s.FechaHoraCierre ?? ahora;

                var duracion =
                    fechaFin - s.FechaHoraInicio;

                return new SesionHistorialResponse
                {
                    SesionId = s.SesionId,
                    UsuarioId = s.UsuarioId,
                    NombreCompleto = s.Usuario.NombreCompleto,
                    NombreUsuario = s.Usuario.NombreUsuario,
                    Rol = s.Usuario.Rol,

                    FechaHoraInicio = s.FechaHoraInicio,
                    FechaHoraCierre = s.FechaHoraCierre,

                    DuracionHoras = Math.Round(
                        (decimal)duracion.TotalHours,
                        2),

                    Estado = s.FechaHoraCierre.HasValue
                        ? "Cerrada"
                        : "Abierta",

                    MotivoCierre = s.MotivoCierre,
                    Equipo = s.Equipo,
                    DireccionIP = s.DireccionIP
                };
            }).ToList();

            return Ok(resultado);
        }
    }
}
