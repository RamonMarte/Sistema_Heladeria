using Heladeria.Api.Data;
using Heladeria.Api.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Api.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Administrador")]
    public class AdminController : ControllerBase
    {
        private readonly HeladeriaDbContext _context;

        public AdminController(HeladeriaDbContext context)
        {
            _context = context;
        }

        // GET: /api/admin/sesiones
        [HttpGet("sesiones")]
        public async Task<IActionResult> ObtenerSesiones(
            [FromQuery] int? usuarioId,
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta)
        {
            if (desde.HasValue &&
                hasta.HasValue &&
                desde.Value.Date > hasta.Value.Date)
            {
                return BadRequest(new
                {
                    mensaje = "La fecha inicial no puede ser posterior a la fecha final."
                });
            }

            IQueryable<HistorialSesion> consulta =
                _context.HistorialSesiones.AsNoTracking();

            if (usuarioId.HasValue)
            {
                consulta = consulta.Where(s =>
                    s.UsuarioId == usuarioId.Value);
            }

            if (desde.HasValue)
            {
                var fechaInicio = desde.Value.Date;

                consulta = consulta.Where(s =>
                    s.FechaHoraInicio >= fechaInicio);
            }

            if (hasta.HasValue)
            {
                var fechaFinalExclusiva =
                    hasta.Value.Date.AddDays(1);

                consulta = consulta.Where(s =>
                    s.FechaHoraInicio < fechaFinalExclusiva);
            }

            var sesiones = await consulta
                .OrderByDescending(s => s.FechaHoraInicio)
                .Take(500)
                .ToListAsync();

            return Ok(sesiones);
        }

        // GET: /api/admin/ventas-por-usuario
        [HttpGet("ventas-por-usuario")]
        public async Task<IActionResult> ObtenerVentasPorUsuario(
            [FromQuery] int? usuarioId,
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta)
        {
            if (desde.HasValue &&
                hasta.HasValue &&
                desde.Value.Date > hasta.Value.Date)
            {
                return BadRequest(new
                {
                    mensaje = "La fecha inicial no puede ser posterior a la fecha final."
                });
            }

            IQueryable<ResumenVentasUsuarioDia> consulta =
                _context.ResumenVentasUsuarioDia.AsNoTracking();

            if (usuarioId.HasValue)
            {
                consulta = consulta.Where(v =>
                    v.UsuarioId == usuarioId.Value);
            }

            if (desde.HasValue)
            {
                var fechaInicio = desde.Value.Date;

                consulta = consulta.Where(v =>
                    v.Fecha >= fechaInicio);
            }

            if (hasta.HasValue)
            {
                var fechaFinalExclusiva =
                    hasta.Value.Date.AddDays(1);

                consulta = consulta.Where(v =>
                    v.Fecha < fechaFinalExclusiva);
            }

            var resumen = await consulta
                .GroupBy(v => new
                {
                    v.UsuarioId,
                    v.NombreCompleto,
                    v.NombreUsuario,
                    v.Rol
                })
                .Select(grupo => new
                {
                    grupo.Key.UsuarioId,
                    grupo.Key.NombreCompleto,
                    grupo.Key.NombreUsuario,
                    grupo.Key.Rol,

                    CantidadVentas =
                        grupo.Sum(v => v.CantidadVentas),

                    CantidadHeladosVendidos =
                        grupo.Sum(v => v.CantidadHeladosVendidos),

                    DineroGenerado =
                        grupo.Sum(v => v.DineroGenerado),

                    GananciaBrutaEstimada =
                        grupo.Sum(v => v.GananciaBrutaEstimada)
                })
                .OrderByDescending(v => v.DineroGenerado)
                .ToListAsync();

            return Ok(resumen);
        }
    }
}
