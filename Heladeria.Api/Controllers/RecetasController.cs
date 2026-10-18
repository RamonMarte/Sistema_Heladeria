using Heladeria.Api.Data;
using Heladeria.Api.Entities;
using Heladeria.Contracts.Recetas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Api.Controllers
{
    [ApiController]
    [Route("api/recetas")]
    [Authorize(Roles = "Administrador,Encargado")]
    public class RecetasController : ControllerBase
    {
        private readonly HeladeriaDbContext _context;

        public RecetasController(HeladeriaDbContext context)
        {
            _context = context;
        }

        // GET: api/recetas/1
        [HttpGet("{productoPresentacionId:int}")]
        public async Task<IActionResult> Obtener(int productoPresentacionId)
        {
            var presentacion = await _context.ProductoPresentaciones
                .AsNoTracking()
                .Where(p => p.ProductoPresentacionId == productoPresentacionId)
                .Select(p => new
                {
                    p.ProductoPresentacionId,
                    Producto = p.Producto.Nombre,
                    Presentacion = p.NombrePresentacion
                })
                .FirstOrDefaultAsync();

            if (presentacion is null)
            {
                return NotFound(new
                {
                    mensaje = "La presentación no existe."
                });
            }

            var ingredientes = await _context.RecetaDetalles
                .AsNoTracking()
                .Where(r =>
                    r.ProductoPresentacionId == productoPresentacionId)
                .OrderBy(r => r.ArticuloInventario.Nombre)
                .Select(r => new RecetaIngredienteResponse
                {
                    ArticuloInventarioId = r.ArticuloInventarioId,
                    NombreArticulo = r.ArticuloInventario.Nombre,
                    TipoArticulo = r.ArticuloInventario.TipoArticulo,
                    UnidadMedida = r.ArticuloInventario.UnidadMedida.Nombre,
                    Abreviatura = r.ArticuloInventario.UnidadMedida.Abreviatura,
                    CantidadPorUnidad = r.CantidadPorUnidad
                })
                .ToListAsync();

            return Ok(new RecetaResponse
            {
                ProductoPresentacionId = presentacion.ProductoPresentacionId,
                Producto = presentacion.Producto,
                Presentacion = presentacion.Presentacion,
                Ingredientes = ingredientes
            });
        }

        // PUT: api/recetas/1
        // Reemplaza la receta completa de una presentación.
        [HttpPut("{productoPresentacionId:int}")]
        public async Task<IActionResult> Actualizar(
            int productoPresentacionId,
            [FromBody] ActualizarRecetaRequest request)
        {
            if (request.Ingredientes.Count == 0 ||
                request.Ingredientes.Count > 50)
            {
                return BadRequest(new
                {
                    mensaje = "La receta debe contener entre 1 y 50 ingredientes."
                });
            }

            var ids = request.Ingredientes
                .Select(i => i.ArticuloInventarioId)
                .ToList();

            if (ids.Distinct().Count() != ids.Count)
            {
                return BadRequest(new
                {
                    mensaje = "No repitas artículos dentro de la receta."
                });
            }

            foreach (var ingrediente in request.Ingredientes)
            {
                if (ingrediente.CantidadPorUnidad <= 0 ||
                    ingrediente.CantidadPorUnidad !=
                        decimal.Round(ingrediente.CantidadPorUnidad, 4))
                {
                    return BadRequest(new
                    {
                        mensaje =
                            "Las cantidades de receta deben ser positivas " +
                            "y admitir como máximo cuatro decimales."
                    });
                }
            }

            var presentacion = await _context.ProductoPresentaciones
                .FirstOrDefaultAsync(p =>
                    p.ProductoPresentacionId == productoPresentacionId &&
                    p.Activa &&
                    p.Producto.Activo &&
                    p.Producto.Categoria.Activa);

            if (presentacion is null)
            {
                return BadRequest(new
                {
                    mensaje =
                        "La presentación, su producto o su categoría están inactivos."
                });
            }

            var articulos = await _context.ArticulosInventario
                .Where(a => ids.Contains(a.ArticuloInventarioId) && a.Activo)
                .ToListAsync();

            if (articulos.Count != ids.Count)
            {
                return BadRequest(new
                {
                    mensaje = "Uno o más artículos no existen o están inactivos."
                });
            }

            if (articulos.Any(a =>
                a.TipoArticulo is not "Ingrediente" and not "Material"))
            {
                return BadRequest(new
                {
                    mensaje =
                        "Las recetas solo pueden consumir ingredientes y materiales. " +
                        "Los productos terminados se controlan mediante su existencia directa."
                });
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var recetaAnterior = await _context.RecetaDetalles
                    .Where(r =>
                        r.ProductoPresentacionId == productoPresentacionId)
                    .ToListAsync();

                _context.RecetaDetalles.RemoveRange(recetaAnterior);

                foreach (var ingrediente in request.Ingredientes)
                {
                    _context.RecetaDetalles.Add(new RecetaDetalle
                    {
                        ProductoPresentacionId = productoPresentacionId,
                        ArticuloInventarioId = ingrediente.ArticuloInventarioId,
                        CantidadPorUnidad = ingrediente.CantidadPorUnidad
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    mensaje = "Receta actualizada correctamente.",
                    productoPresentacionId,
                    ingredientes = request.Ingredientes.Count
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
