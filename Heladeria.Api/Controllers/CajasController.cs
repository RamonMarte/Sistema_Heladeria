using Heladeria.Api.Data;
using Heladeria.Api.Entities;
using Heladeria.Contracts.Cajas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Api.Controllers
{
    [ApiController]
    [Route("api/cajas")]
    [Authorize]
    public class CajasController : ControllerBase
    {
        private readonly HeladeriaDbContext _context;

        public CajasController(HeladeriaDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodas(
            [FromQuery] bool incluirInactivas = false)
        {
            if (incluirInactivas &&
                !User.IsInRole("Administrador"))
            {
                return Forbid();
            }

            var consulta = _context.Cajas
                .AsNoTracking()
                .AsQueryable();

            if (!incluirInactivas)
                consulta = consulta.Where(c => c.Activa);

            var cajas = await consulta
                .OrderBy(c => c.Nombre)
                .Select(c => new CajaResponse
                {
                    CajaId = c.CajaId,
                    Codigo = c.Codigo,
                    Nombre = c.Nombre,
                    Activa = c.Activa
                })
                .ToListAsync();

            return Ok(cajas);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Crear(
            [FromBody] CrearCajaRequest request)
        {
            var codigo = request.Codigo.Trim();
            var nombre = request.Nombre.Trim();

            if (await _context.Cajas.AnyAsync(c =>
                    c.Codigo == codigo || c.Nombre == nombre))
            {
                return Conflict(new
                {
                    mensaje = "Ya existe una caja con ese código o nombre."
                });
            }

            var caja = new Caja
            {
                Codigo = codigo,
                Nombre = nombre,
                Activa = true
            };

            _context.Cajas.Add(caja);
            await _context.SaveChangesAsync();

            return Ok(new CajaResponse
            {
                CajaId = caja.CajaId,
                Codigo = caja.Codigo,
                Nombre = caja.Nombre,
                Activa = caja.Activa
            });
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Actualizar(
            int id,
            [FromBody] ActualizarCajaRequest request)
        {
            var caja = await _context.Cajas
                .FirstOrDefaultAsync(c => c.CajaId == id);

            if (caja is null)
            {
                return NotFound(new
                {
                    mensaje = "La caja no existe."
                });
            }

            var codigo = request.Codigo.Trim();
            var nombre = request.Nombre.Trim();

            var duplicada = await _context.Cajas.AnyAsync(c =>
                c.CajaId != id &&
                (c.Codigo == codigo || c.Nombre == nombre));

            if (duplicada)
            {
                return Conflict(new
                {
                    mensaje = "El código o nombre ya pertenece a otra caja."
                });
            }

            caja.Codigo = codigo;
            caja.Nombre = nombre;
            caja.Activa = request.Activa;

            await _context.SaveChangesAsync();

            return Ok(new CajaResponse
            {
                CajaId = caja.CajaId,
                Codigo = caja.Codigo,
                Nombre = caja.Nombre,
                Activa = caja.Activa
            });
        }
    }
}
