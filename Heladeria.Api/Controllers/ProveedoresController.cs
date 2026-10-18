using Heladeria.Api.Data;
using Heladeria.Api.Entities;
using Heladeria.Contracts.Proveedores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Api.Controllers
{
    [ApiController]
    [Route("api/proveedores")]
    [Authorize(Roles = "Administrador,Encargado")]
    public class ProveedoresController : ControllerBase
    {
        private readonly HeladeriaDbContext _context;

        public ProveedoresController(HeladeriaDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerTodos(
            [FromQuery] bool incluirInactivos = false)
        {
            var consulta = _context.Proveedores
                .AsNoTracking()
                .AsQueryable();

            if (!incluirInactivos)
                consulta = consulta.Where(p => p.Activo);

            var proveedores = await consulta
                .OrderBy(p => p.Nombre)
                .Select(p => new ProveedorResponse
                {
                    ProveedorId = p.ProveedorId,
                    Nombre = p.Nombre,
                    RNC = p.RNC,
                    Telefono = p.Telefono,
                    Correo = p.Correo,
                    Direccion = p.Direccion,
                    Activo = p.Activo,
                    FechaCreacion = p.FechaCreacion
                })
                .ToListAsync();

            return Ok(proveedores);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var proveedor = await _context.Proveedores
                .AsNoTracking()
                .Where(p => p.ProveedorId == id)
                .Select(p => new ProveedorResponse
                {
                    ProveedorId = p.ProveedorId,
                    Nombre = p.Nombre,
                    RNC = p.RNC,
                    Telefono = p.Telefono,
                    Correo = p.Correo,
                    Direccion = p.Direccion,
                    Activo = p.Activo,
                    FechaCreacion = p.FechaCreacion
                })
                .FirstOrDefaultAsync();

            if (proveedor is null)
            {
                return NotFound(new
                {
                    mensaje = "El proveedor no existe."
                });
            }

            return Ok(proveedor);
        }

        [HttpPost]
        public async Task<IActionResult> Crear(
            [FromBody] ProveedorRequest request)
        {
            var nombre = request.Nombre.Trim();

            var proveedor = new Proveedor
            {
                Nombre = nombre,
                RNC = Limpiar(request.RNC),
                Telefono = Limpiar(request.Telefono),
                Correo = Limpiar(request.Correo),
                Direccion = Limpiar(request.Direccion),
                Activo = true
            };

            _context.Proveedores.Add(proveedor);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(ObtenerPorId),
                new { id = proveedor.ProveedorId },
                ConvertirRespuesta(proveedor));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Actualizar(
            int id,
            [FromBody] ProveedorRequest request)
        {
            var proveedor = await _context.Proveedores
                .FirstOrDefaultAsync(p => p.ProveedorId == id);

            if (proveedor is null)
            {
                return NotFound(new
                {
                    mensaje = "El proveedor no existe."
                });
            }

            proveedor.Nombre = request.Nombre.Trim();
            proveedor.RNC = Limpiar(request.RNC);
            proveedor.Telefono = Limpiar(request.Telefono);
            proveedor.Correo = Limpiar(request.Correo);
            proveedor.Direccion = Limpiar(request.Direccion);
            proveedor.Activo = request.Activo;

            await _context.SaveChangesAsync();

            return Ok(ConvertirRespuesta(proveedor));
        }

        private static string? Limpiar(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor)
                ? null
                : valor.Trim();
        }

        private static ProveedorResponse ConvertirRespuesta(
            Proveedor proveedor)
        {
            return new ProveedorResponse
            {
                ProveedorId = proveedor.ProveedorId,
                Nombre = proveedor.Nombre,
                RNC = proveedor.RNC,
                Telefono = proveedor.Telefono,
                Correo = proveedor.Correo,
                Direccion = proveedor.Direccion,
                Activo = proveedor.Activo,
                FechaCreacion = proveedor.FechaCreacion
            };
        }
    }
}
