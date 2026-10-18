using Heladeria.Api.Data;
using Heladeria.Api.Entities;
using Heladeria.Contracts.Productos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Api.Controllers
{
    [ApiController]
    [Route("api/productos")]
    [Authorize]
    public class ProductosController : ControllerBase
    {
        private readonly HeladeriaDbContext _context;

        public ProductosController(HeladeriaDbContext context)
        {
            _context = context;
        }

        // GET: api/productos
        [HttpGet]
        public async Task<IActionResult> ObtenerProductos(
            [FromQuery] string? buscar,
            [FromQuery] int? categoriaId,
            [FromQuery] bool incluirInactivos = false)
        {
            bool esAdministrador =
                User.IsInRole("Administrador") ||
                User.IsInRole("Encargado");

            if (incluirInactivos && !esAdministrador)
                return Forbid();

            if (buscar?.Length > 100)
            {
                return BadRequest(new
                {
                    mensaje = "El texto de búsqueda no puede superar 100 caracteres."
                });
            }

            var consulta = _context.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Include(p => p.Presentaciones)
                .AsQueryable();

            if (!incluirInactivos)
            {
                consulta = consulta.Where(p =>
                    p.Activo && p.Categoria.Activa);
            }

            if (categoriaId.HasValue)
            {
                consulta = consulta.Where(p =>
                    p.CategoriaId == categoriaId.Value);
            }

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var texto = buscar.Trim();

                consulta = consulta.Where(p =>
                    p.Nombre.Contains(texto) ||
                    (p.Sabor != null && p.Sabor.Contains(texto)));
            }

            var productos = await consulta
                .OrderBy(p => p.Nombre)
                .Take(500)
                .ToListAsync();

            var respuesta = productos
                .Select(p => ConvertirProducto(p, incluirInactivos))
                .ToList();

            return Ok(respuesta);
        }

        // GET: api/productos/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> ObtenerPorId(int id)
        {
            var producto = await _context.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .Include(p => p.Presentaciones)
                .FirstOrDefaultAsync(p => p.ProductoId == id);

            if (producto is null)
                return NotFound(new { mensaje = "El producto no existe." });

            bool esAdministrador =
                User.IsInRole("Administrador") ||
                User.IsInRole("Encargado");

            if ((!producto.Activo || !producto.Categoria.Activa) &&
                !esAdministrador)
            {
                return NotFound();
            }

            return Ok(ConvertirProducto(producto, esAdministrador));
        }

        // GET: api/productos/catalogo?buscar=chocolate
        [HttpGet("catalogo")]
        public async Task<IActionResult> ObtenerCatalogoVenta(
            [FromQuery] string? buscar,
            [FromQuery] int? categoriaId)
        {
            if (buscar?.Length > 100)
            {
                return BadRequest(new
                {
                    mensaje = "El texto de búsqueda no puede superar 100 caracteres."
                });
            }

            var consulta = _context.ProductoPresentaciones
                .AsNoTracking()
                .Where(pp =>
                    pp.Activa &&
                    pp.Producto.Activo &&
                    pp.Producto.Categoria.Activa);

            if (categoriaId.HasValue)
            {
                consulta = consulta.Where(pp =>
                    pp.Producto.CategoriaId == categoriaId.Value);
            }

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                var texto = buscar.Trim();

                consulta = consulta.Where(pp =>
                    pp.CodigoInterno == texto ||
                    (pp.CodigoBarras != null &&
                     pp.CodigoBarras == texto) ||
                    pp.Producto.Nombre.Contains(texto) ||
                    (pp.Producto.Sabor != null &&
                     pp.Producto.Sabor.Contains(texto)) ||
                    pp.NombrePresentacion.Contains(texto) ||
                    pp.Producto.Categoria.Nombre.Contains(texto));
            }

            var productos = await consulta
                .OrderBy(pp => pp.Producto.Nombre)
                .ThenBy(pp => pp.NombrePresentacion)
                .Take(500)
                .Select(pp => new CatalogoVentaResponse
                {
                    ProductoPresentacionId =
                        pp.ProductoPresentacionId,

                    ProductoId = pp.ProductoId,

                    NombreProducto = pp.Producto.Nombre,

                    Sabor = pp.Producto.Sabor,

                    Categoria = pp.Producto.Categoria.Nombre,

                    NombrePresentacion = pp.NombrePresentacion,

                    Tamano = pp.Tamano,

                    CodigoInterno = pp.CodigoInterno,

                    CodigoBarras = pp.CodigoBarras,

                    PrecioVenta = pp.PrecioVenta
                })
                .ToListAsync();

            return Ok(productos);
        }

        // POST: api/productos
        [HttpPost]
        [Authorize(Roles = "Administrador,Encargado")]
        public async Task<IActionResult> Crear(
            [FromBody] CrearProductoRequest request)
        {
            var nombre = request.Nombre.Trim();

            if (string.IsNullOrWhiteSpace(nombre))
            {
                return BadRequest(new
                {
                    mensaje = "El nombre del producto es obligatorio."
                });
            }

            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(c =>
                    c.CategoriaId == request.CategoriaId &&
                    c.Activa);

            if (categoria is null)
            {
                return BadRequest(new
                {
                    mensaje = "La categoría no existe o está inactiva."
                });
            }

            var producto = new Producto
            {
                CategoriaId = categoria.CategoriaId,
                Nombre = nombre,
                Sabor = Limpiar(request.Sabor),
                Descripcion = Limpiar(request.Descripcion),
                Activo = true
            };

            _context.Productos.Add(producto);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(ObtenerPorId),
                new { id = producto.ProductoId },
                new
                {
                    producto.ProductoId,
                    producto.CategoriaId,
                    CategoriaNombre = categoria.Nombre,
                    producto.Nombre,
                    producto.Sabor,
                    producto.Descripcion,
                    producto.Activo,
                    producto.FechaCreacion
                });
        }

        // PUT: api/productos/1
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Administrador,Encargado")]
        public async Task<IActionResult> Actualizar(
            int id,
            [FromBody] ActualizarProductoRequest request)
        {
            var producto = await _context.Productos
                .FirstOrDefaultAsync(p => p.ProductoId == id);

            if (producto is null)
                return NotFound(new { mensaje = "El producto no existe." });

            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(c =>
                    c.CategoriaId == request.CategoriaId &&
                    c.Activa);

            if (categoria is null)
            {
                return BadRequest(new
                {
                    mensaje = "La categoría no existe o está inactiva."
                });
            }

            producto.CategoriaId = categoria.CategoriaId;
            producto.Nombre = request.Nombre.Trim();
            producto.Sabor = Limpiar(request.Sabor);
            producto.Descripcion = Limpiar(request.Descripcion);
            producto.Activo = request.Activo;
            producto.FechaActualizacion = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                mensaje = "Producto actualizado correctamente.",
                producto.ProductoId,
                producto.Nombre,
                producto.Activo
            });
        }

        // POST: api/productos/1/presentaciones
        [HttpPost("{productoId:int}/presentaciones")]
        [Authorize(Roles = "Administrador,Encargado")]
        public async Task<IActionResult> CrearPresentacion(
            int productoId,
            [FromBody] CrearPresentacionRequest request)
        {
            var producto = await _context.Productos
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p =>
                    p.ProductoId == productoId &&
                    p.Activo &&
                    p.Categoria.Activa);

            if (producto is null)
            {
                return NotFound(new
                {
                    mensaje = "El producto no existe o está inactivo."
                });
            }

            var codigo = request.CodigoInterno.Trim();
            var barras = Limpiar(request.CodigoBarras);

            if (await _context.ProductoPresentaciones
                .AnyAsync(p => p.CodigoInterno == codigo))
            {
                return Conflict(new
                {
                    mensaje = "Ese código interno ya está registrado."
                });
            }

            if (barras != null &&
                await _context.ProductoPresentaciones
                    .AnyAsync(p => p.CodigoBarras == barras))
            {
                return Conflict(new
                {
                    mensaje = "Ese código de barras ya está registrado."
                });
            }

            var presentacion = new ProductoPresentacion
            {
                ProductoId = productoId,
                NombrePresentacion = request.NombrePresentacion.Trim(),
                Tamano = Limpiar(request.Tamano),
                CodigoInterno = codigo,
                CodigoBarras = barras,
                PrecioVenta = request.PrecioVenta,
                CostoEstimado = request.CostoEstimado,
                Activa = true
            };

            _context.ProductoPresentaciones.Add(presentacion);

            await _context.SaveChangesAsync();

            return Ok(ConvertirPresentacion(presentacion));
        }

        // PUT: api/productos/presentaciones/1
        [HttpPut("presentaciones/{id:int}")]
        [Authorize(Roles = "Administrador,Encargado")]
        public async Task<IActionResult> ActualizarPresentacion(
            int id,
            [FromBody] ActualizarPresentacionRequest request)
        {
            var presentacion = await _context.ProductoPresentaciones
                .Include(p => p.Producto)
                    .ThenInclude(p => p.Categoria)
                .FirstOrDefaultAsync(p =>
                    p.ProductoPresentacionId == id);

            if (presentacion is null)
            {
                return NotFound(new
                {
                    mensaje = "La presentación no existe."
                });
            }

            if (request.Activa &&
                (!presentacion.Producto.Activo ||
                 !presentacion.Producto.Categoria.Activa))
            {
                return BadRequest(new
                {
                    mensaje =
                        "No puedes activar la presentación porque " +
                        "el producto o su categoría están inactivos."
                });
            }

            var codigo = request.CodigoInterno.Trim();
            var barras = Limpiar(request.CodigoBarras);

            if (await _context.ProductoPresentaciones.AnyAsync(p =>
                    p.ProductoPresentacionId != id &&
                    p.CodigoInterno == codigo))
            {
                return Conflict(new
                {
                    mensaje = "Ese código interno ya está registrado."
                });
            }

            if (barras != null &&
                await _context.ProductoPresentaciones.AnyAsync(p =>
                    p.ProductoPresentacionId != id &&
                    p.CodigoBarras == barras))
            {
                return Conflict(new
                {
                    mensaje = "Ese código de barras ya está registrado."
                });
            }

            presentacion.NombrePresentacion =
                request.NombrePresentacion.Trim();

            presentacion.Tamano = Limpiar(request.Tamano);
            presentacion.CodigoInterno = codigo;
            presentacion.CodigoBarras = barras;
            presentacion.PrecioVenta = request.PrecioVenta;
            presentacion.CostoEstimado = request.CostoEstimado;
            presentacion.Activa = request.Activa;

            await _context.SaveChangesAsync();

            return Ok(ConvertirPresentacion(presentacion));
        }

        private static string? Limpiar(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor)
                ? null
                : valor.Trim();
        }

        private static ProductoResponse ConvertirProducto(
            Producto producto,
            bool incluirInactivos)
        {
            return new ProductoResponse
            {
                ProductoId = producto.ProductoId,
                CategoriaId = producto.CategoriaId,
                CategoriaNombre = producto.Categoria.Nombre,
                Nombre = producto.Nombre,
                Sabor = producto.Sabor,
                Descripcion = producto.Descripcion,
                Activo = producto.Activo,
                FechaCreacion = producto.FechaCreacion,
                FechaActualizacion = producto.FechaActualizacion,

                Presentaciones = producto.Presentaciones
                    .Where(p => incluirInactivos || p.Activa)
                    .OrderBy(p => p.NombrePresentacion)
                    .Select(ConvertirPresentacion)
                    .ToList()
            };
        }

        private static PresentacionResponse ConvertirPresentacion(
            ProductoPresentacion presentacion)
        {
            return new PresentacionResponse
            {
                ProductoPresentacionId =
                    presentacion.ProductoPresentacionId,

                NombrePresentacion =
                    presentacion.NombrePresentacion,

                Tamano = presentacion.Tamano,

                CodigoInterno = presentacion.CodigoInterno,

                CodigoBarras = presentacion.CodigoBarras,

                PrecioVenta = presentacion.PrecioVenta,

                CostoEstimado = presentacion.CostoEstimado,

                Activa = presentacion.Activa,

                FechaCreacion = presentacion.FechaCreacion
            };
        }
    }
}
