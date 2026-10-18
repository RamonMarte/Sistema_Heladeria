using Heladeria.Api.Data;
using Heladeria.Api.Entities;
using Heladeria.Contracts.Categorias;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Api.Controllers
{
    [ApiController]
    [Route("api/categorias")]
    [Authorize]
    public class CategoriasController : ControllerBase
    {
        private readonly HeladeriaDbContext _context;

        public CategoriasController(HeladeriaDbContext context)
        {
            _context = context;
        }

        // GET: api/categorias
        [HttpGet]
        public async Task<IActionResult> ObtenerTodas(
            [FromQuery] bool incluirInactivas = false)
        {
            bool puedeAdministrar =
                User.IsInRole("Administrador") ||
                User.IsInRole("Encargado");

            if (incluirInactivas && !puedeAdministrar)
            {
                return Forbid();
            }

            var consulta = _context.Categorias
                .AsNoTracking()
                .AsQueryable();

            if (!incluirInactivas)
            {
                consulta = consulta.Where(c => c.Activa);
            }

            var categorias = await consulta
                .OrderBy(c => c.Nombre)
                .Select(c => new CategoriaResponse
                {
                    CategoriaId = c.CategoriaId,
                    Nombre = c.Nombre,
                    Descripcion = c.Descripcion,
                    Activa = c.Activa,
                    FechaCreacion = c.FechaCreacion
                })
                .ToListAsync();

            return Ok(categorias);
        }

        // GET: api/categorias/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var categoria = await _context.Categorias
                .AsNoTracking()
                .Where(c => c.CategoriaId == id)
                .Select(c => new CategoriaResponse
                {
                    CategoriaId = c.CategoriaId,
                    Nombre = c.Nombre,
                    Descripcion = c.Descripcion,
                    Activa = c.Activa,
                    FechaCreacion = c.FechaCreacion
                })
                .FirstOrDefaultAsync();

            if (categoria is null)
            {
                return NotFound(new
                {
                    mensaje = "La categoría no existe."
                });
            }

            if (!categoria.Activa &&
                !User.IsInRole("Administrador") &&
                !User.IsInRole("Encargado"))
            {
                return NotFound();
            }

            return Ok(categoria);
        }

        // POST: api/categorias
        [HttpPost]
        [Authorize(Roles = "Administrador,Encargado")]
        public async Task<IActionResult> Crear(
            [FromBody] CrearCategoriaRequest request)
        {
            var nombre = request.Nombre.Trim();

            if (string.IsNullOrWhiteSpace(nombre))
            {
                return BadRequest(new
                {
                    mensaje = "El nombre de la categoría es obligatorio."
                });
            }

            var existe = await _context.Categorias
                .AnyAsync(c => c.Nombre == nombre);

            if (existe)
            {
                return Conflict(new
                {
                    mensaje = "Ya existe una categoría con ese nombre."
                });
            }

            var categoria = new Categoria
            {
                Nombre = nombre,
                Descripcion = string.IsNullOrWhiteSpace(request.Descripcion)
                    ? null
                    : request.Descripcion.Trim(),
                Activa = true
            };

            _context.Categorias.Add(categoria);

            await _context.SaveChangesAsync();

            var respuesta = ConvertirRespuesta(categoria);

            return CreatedAtAction(
                nameof(ObtenerPorId),
                new { id = categoria.CategoriaId },
                respuesta);
        }

        // PUT: api/categorias/1
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrador,Encargado")]
        public async Task<IActionResult> Actualizar(
            int id,
            [FromBody] ActualizarCategoriaRequest request)
        {
            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(c => c.CategoriaId == id);

            if (categoria is null)
            {
                return NotFound(new
                {
                    mensaje = "La categoría no existe."
                });
            }

            var nombre = request.Nombre.Trim();

            if (string.IsNullOrWhiteSpace(nombre))
            {
                return BadRequest(new
                {
                    mensaje = "El nombre de la categoría es obligatorio."
                });
            }

            var nombreDuplicado = await _context.Categorias
                .AnyAsync(c =>
                    c.CategoriaId != id &&
                    c.Nombre == nombre);

            if (nombreDuplicado)
            {
                return Conflict(new
                {
                    mensaje = "Ya existe otra categoría con ese nombre."
                });
            }

            if (!request.Activa)
            {
                var tieneProductosActivos = await _context.Productos
                    .AnyAsync(p =>
                        p.CategoriaId == id &&
                        p.Activo);

                if (tieneProductosActivos)
                {
                    return BadRequest(new
                    {
                        mensaje =
                            "No puedes desactivar esta categoría porque " +
                            "tiene productos activos. Desactiva primero " +
                            "sus productos o muévelos a otra categoría."
                    });
                }
            }

            categoria.Nombre = nombre;

            categoria.Descripcion =
                string.IsNullOrWhiteSpace(request.Descripcion)
                    ? null
                    : request.Descripcion.Trim();

            categoria.Activa = request.Activa;

            await _context.SaveChangesAsync();

            return Ok(ConvertirRespuesta(categoria));
        }

        private static CategoriaResponse ConvertirRespuesta(
            Categoria categoria)
        {
            return new CategoriaResponse
            {
                CategoriaId = categoria.CategoriaId,
                Nombre = categoria.Nombre,
                Descripcion = categoria.Descripcion,
                Activa = categoria.Activa,
                FechaCreacion = categoria.FechaCreacion
            };
        }
    }
}
