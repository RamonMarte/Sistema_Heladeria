using Heladeria.Api.Data;
using Heladeria.Api.Entities;
using Heladeria.Contracts.Inventario;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Claims;

namespace Heladeria.Api.Controllers
{
    [ApiController]
    [Route("api/inventario")]
    [Authorize]
    public class InventarioController : ControllerBase
    {
        private readonly HeladeriaDbContext _context;

        public InventarioController(HeladeriaDbContext context)
        {
            _context = context;
        }

        // GET: api/inventario/unidades
        [HttpGet("unidades")]
        public async Task<IActionResult> ObtenerUnidades()
        {
            var unidades = await _context.UnidadesMedida
                .AsNoTracking()
                .Where(u => u.Activa)
                .OrderBy(u => u.Nombre)
                .Select(u => new
                {
                    u.UnidadMedidaId,
                    u.Nombre,
                    u.Abreviatura
                })
                .ToListAsync();

            return Ok(unidades);
        }

        // GET: api/inventario/articulos
        [HttpGet("articulos")]
        [Authorize(Roles = "Administrador,Encargado")]
        public async Task<IActionResult> ObtenerArticulos(
            [FromQuery] bool incluirInactivos = false)
        {
            var consulta = _context.ArticulosInventario
                .AsNoTracking()
                .Include(a => a.UnidadMedida)
                .AsQueryable();

            if (!incluirInactivos)
            {
                consulta = consulta.Where(a => a.Activo);
            }

            var articulos = await consulta
                .OrderBy(a => a.Nombre)
                .Select(a => new ArticuloInventarioResponse
                {
                    ArticuloInventarioId = a.ArticuloInventarioId,
                    Nombre = a.Nombre,
                    TipoArticulo = a.TipoArticulo,
                    UnidadMedidaId = a.UnidadMedidaId,
                    UnidadMedida = a.UnidadMedida.Nombre,
                    Abreviatura = a.UnidadMedida.Abreviatura,
                    ProductoPresentacionId = a.ProductoPresentacionId,
                    StockMinimo = a.StockMinimo,
                    Activo = a.Activo,
                    FechaCreacion = a.FechaCreacion
                })
                .ToListAsync();

            return Ok(articulos);
        }

        // GET: api/inventario/articulos/1
        [HttpGet("articulos/{id:int}")]
        [Authorize(Roles = "Administrador,Encargado")]
        public async Task<IActionResult> ObtenerArticuloPorId(int id)
        {
            var articulo = await _context.ArticulosInventario
                .AsNoTracking()
                .Where(a => a.ArticuloInventarioId == id)
                .Select(a => new ArticuloInventarioResponse
                {
                    ArticuloInventarioId = a.ArticuloInventarioId,
                    Nombre = a.Nombre,
                    TipoArticulo = a.TipoArticulo,
                    UnidadMedidaId = a.UnidadMedidaId,
                    UnidadMedida = a.UnidadMedida.Nombre,
                    Abreviatura = a.UnidadMedida.Abreviatura,
                    ProductoPresentacionId = a.ProductoPresentacionId,
                    StockMinimo = a.StockMinimo,
                    Activo = a.Activo,
                    FechaCreacion = a.FechaCreacion
                })
                .FirstOrDefaultAsync();

            if (articulo is null)
            {
                return NotFound(new
                {
                    mensaje = "El artículo de inventario no existe."
                });
            }

            return Ok(articulo);
        }

        // GET: api/inventario/existencias
        [HttpGet("existencias")]
        [Authorize(Roles = "Administrador,Encargado")]
        public async Task<IActionResult> ObtenerExistencias(
            [FromQuery] bool soloBajoMinimo = false)
        {
            var consulta = _context.ExistenciasInventario
                .AsNoTracking()
                .AsQueryable();

            if (soloBajoMinimo)
            {
                consulta = consulta.Where(e => e.RequiereReposicion);
            }

            var existencias = await consulta
                .OrderByDescending(e => e.RequiereReposicion)
                .ThenBy(e => e.Nombre)
                .Select(e => new ExistenciaInventarioResponse
                {
                    ArticuloInventarioId = e.ArticuloInventarioId,
                    Nombre = e.Nombre,
                    TipoArticulo = e.TipoArticulo,
                    UnidadMedida = e.UnidadMedida,
                    Abreviatura = e.Abreviatura,
                    StockMinimo = e.StockMinimo,
                    ExistenciaActual = e.ExistenciaActual,
                    RequiereReposicion = e.RequiereReposicion
                })
                .ToListAsync();

            return Ok(existencias);
        }

        // POST: api/inventario/articulos
        [HttpPost("articulos")]
        [Authorize(Roles = "Administrador,Encargado")]
        public async Task<IActionResult> CrearArticulo(
            [FromBody] CrearArticuloInventarioRequest request)
        {
            var tipo = request.TipoArticulo.Trim();

            if (tipo is not "Ingrediente"
                and not "Material"
                and not "ProductoTerminado")
            {
                return BadRequest(new
                {
                    mensaje = "El tipo de artículo no es válido."
                });
            }

            var unidad = await _context.UnidadesMedida
                .FirstOrDefaultAsync(u =>
                    u.UnidadMedidaId == request.UnidadMedidaId &&
                    u.Activa);

            if (unidad is null)
            {
                return BadRequest(new
                {
                    mensaje = "La unidad de medida no existe o está inactiva."
                });
            }

            string nombre;

            if (tipo == "ProductoTerminado")
            {
                if (!request.ProductoPresentacionId.HasValue)
                {
                    return BadRequest(new
                    {
                        mensaje =
                            "Debes seleccionar una presentación de producto."
                    });
                }

                var presentacion = await _context.ProductoPresentaciones
                    .Include(p => p.Producto)
                        .ThenInclude(p => p.Categoria)
                    .FirstOrDefaultAsync(p =>
                        p.ProductoPresentacionId ==
                        request.ProductoPresentacionId.Value &&
                        p.Activa &&
                        p.Producto.Activo &&
                        p.Producto.Categoria.Activa);

                if (presentacion is null)
                {
                    return BadRequest(new
                    {
                        mensaje =
                            "La presentación no existe o está inactiva."
                    });
                }

                var yaRegistrado = await _context.ArticulosInventario
                    .AnyAsync(a =>
                        a.ProductoPresentacionId ==
                        request.ProductoPresentacionId.Value);

                if (yaRegistrado)
                {
                    return Conflict(new
                    {
                        mensaje =
                            "Esta presentación ya está vinculada al inventario."
                    });
                }

                nombre =
                    $"{presentacion.Producto.Nombre} - " +
                    presentacion.NombrePresentacion;
            }
            else
            {
                if (request.ProductoPresentacionId.HasValue)
                {
                    return BadRequest(new
                    {
                        mensaje =
                            "Los ingredientes y materiales no deben " +
                            "vincularse directamente a una presentación vendible."
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Nombre) ||
                    request.Nombre.Trim().Length < 2)
                {
                    return BadRequest(new
                    {
                        mensaje =
                            "El nombre del ingrediente o material es obligatorio."
                    });
                }

                nombre = request.Nombre.Trim();

                var duplicado = await _context.ArticulosInventario
                    .AnyAsync(a =>
                        a.Activo &&
                        a.Nombre == nombre &&
                        a.TipoArticulo == tipo);

                if (duplicado)
                {
                    return Conflict(new
                    {
                        mensaje =
                            "Ya existe un artículo activo de este tipo con ese nombre."
                    });
                }
            }

            var articulo = new ArticuloInventario
            {
                Nombre = nombre,
                TipoArticulo = tipo,
                UnidadMedidaId = unidad.UnidadMedidaId,
                ProductoPresentacionId =
                    tipo == "ProductoTerminado"
                        ? request.ProductoPresentacionId
                        : null,
                StockMinimo = request.StockMinimo,
                Activo = true
            };

            _context.ArticulosInventario.Add(articulo);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(ObtenerArticuloPorId),
                new { id = articulo.ArticuloInventarioId },
                new
                {
                    articulo.ArticuloInventarioId,
                    articulo.Nombre,
                    articulo.TipoArticulo,
                    articulo.UnidadMedidaId,
                    UnidadMedida = unidad.Nombre,
                    Abreviatura = unidad.Abreviatura,
                    articulo.ProductoPresentacionId,
                    articulo.StockMinimo,
                    articulo.Activo,
                    articulo.FechaCreacion
                });
        }

        // POST: api/inventario/mermas
        [HttpPost("mermas")]
        [Authorize(Roles = "Administrador,Encargado")]
        public async Task<IActionResult> RegistrarMerma(
            [FromBody] RegistrarMermaRequest request)
        {
            var usuarioTexto =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(usuarioTexto, out var usuarioId))
            {
                return Unauthorized(new
                {
                    mensaje = "No se pudo identificar al usuario."
                });
            }

            if (request.Cantidad <= 0 ||
                request.Cantidad != decimal.Round(request.Cantidad, 3))
            {
                return BadRequest(new
                {
                    mensaje = "La cantidad debe ser positiva y admitir como máximo tres decimales."
                });
            }

            await using var transaccion =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var articuloExiste = await _context.ArticulosInventario
                    .AnyAsync(a =>
                        a.ArticuloInventarioId == request.ArticuloInventarioId &&
                        a.Activo);

                if (!articuloExiste)
                {
                    return NotFound(new
                    {
                        mensaje = "El artículo no existe o está inactivo."
                    });
                }

                var observaciones =
                    $"Merma: {request.Motivo}. " +
                    $"{request.Observaciones}".Trim();

                var resultado = await RegistrarSalidaPorLotesAsync(
                    articuloId: request.ArticuloInventarioId,
                    cantidad: request.Cantidad,
                    tipoMovimiento: "Merma",
                    referenciaTipo: "Merma",
                    usuarioId: usuarioId,
                    observaciones: observaciones,
                    loteId: request.LoteInventarioId,
                    soloVencidos: request.Motivo == "Vencido");

                if (!resultado.Exito)
                {
                    return BadRequest(new
                    {
                        mensaje = resultado.Error
                    });
                }

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                return Ok(new
                {
                    mensaje = "Merma registrada correctamente.",
                    articuloInventarioId = request.ArticuloInventarioId,
                    cantidad = request.Cantidad,
                    motivo = request.Motivo,
                    lotesAfectados = resultado.LotesAfectados
                });
            }
            catch
            {
                await transaccion.RollbackAsync();
                throw;
            }
        }

        // POST: api/inventario/ajustes
        [HttpPost("ajustes")]
        [Authorize(Roles = "Administrador,Encargado")]
        public async Task<IActionResult> RegistrarAjuste(
            [FromBody] RegistrarAjusteInventarioRequest request)
        {
            var usuarioTexto =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(usuarioTexto, out var usuarioId))
            {
                return Unauthorized(new
                {
                    mensaje = "No se pudo identificar al usuario."
                });
            }

            if (request.CantidadContada < 0 ||
                request.CantidadContada !=
                    decimal.Round(request.CantidadContada, 3))
            {
                return BadRequest(new
                {
                    mensaje =
                        "La cantidad contada no puede ser negativa " +
                        "y admite como máximo tres decimales."
                });
            }

            if (request.CostoUnitarioEntrada.HasValue &&
                (request.CostoUnitarioEntrada.Value < 0 ||
                 request.CostoUnitarioEntrada.Value !=
                    decimal.Round(request.CostoUnitarioEntrada.Value, 4)))
            {
                return BadRequest(new
                {
                    mensaje =
                        "El costo de entrada debe ser no negativo " +
                        "y admitir como máximo cuatro decimales."
                });
            }

            if (request.FechaVencimiento.HasValue &&
                request.FechaVencimiento.Value <
                    DateOnly.FromDateTime(DateTime.Today))
            {
                return BadRequest(new
                {
                    mensaje = "La fecha de vencimiento no puede estar en el pasado."
                });
            }

            await using var transaccion =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var articulo = await _context.ArticulosInventario
                    .FirstOrDefaultAsync(a =>
                        a.ArticuloInventarioId ==
                            request.ArticuloInventarioId &&
                        a.Activo);

                if (articulo is null)
                {
                    return NotFound(new
                    {
                        mensaje = "El artículo no existe o está inactivo."
                    });
                }

                var existenciaActual =
                    await _context.LotesInventario
                        .Where(l =>
                            l.ArticuloInventarioId ==
                                request.ArticuloInventarioId &&
                            l.Activo)
                        .SumAsync(l => (decimal?)l.CantidadActual)
                    ?? 0m;

                var diferencia =
                    request.CantidadContada - existenciaActual;

                if (diferencia == 0)
                {
                    await transaccion.CommitAsync();

                    return Ok(new
                    {
                        mensaje = "El conteo coincide con la existencia actual.",
                        existenciaAnterior = existenciaActual,
                        existenciaNueva = request.CantidadContada,
                        diferencia = 0m,
                        ajusteRealizado = false
                    });
                }

                if (diferencia < 0)
                {
                    var cantidadARetirar = Math.Abs(diferencia);

                    var observaciones =
                        $"Ajuste por conteo físico. " +
                        $"Motivo: {request.Motivo.Trim()}. " +
                        $"Existencia anterior: {existenciaActual}. " +
                        $"Existencia contada: {request.CantidadContada}. " +
                        $"{request.Observaciones}".Trim();

                    var resultado = await RegistrarSalidaPorLotesAsync(
                        articuloId: request.ArticuloInventarioId,
                        cantidad: cantidadARetirar,
                        tipoMovimiento: "Ajuste",
                        referenciaTipo: "AjusteInventario",
                        usuarioId: usuarioId,
                        observaciones: observaciones,
                        loteId: null,
                        soloVencidos: false);

                    if (!resultado.Exito)
                    {
                        return BadRequest(new
                        {
                            mensaje = resultado.Error
                        });
                    }

                    await _context.SaveChangesAsync();
                    await transaccion.CommitAsync();

                    return Ok(new
                    {
                        mensaje = "Ajuste de salida registrado correctamente.",
                        existenciaAnterior = existenciaActual,
                        existenciaNueva = request.CantidadContada,
                        diferencia,
                        lotesAfectados = resultado.LotesAfectados
                    });
                }

                // Si el conteo real supera la existencia registrada,
                // se debe conocer el costo para valorar la entrada.
                if (!request.CostoUnitarioEntrada.HasValue)
                {
                    return BadRequest(new
                    {
                        mensaje =
                            "Como la cantidad contada supera la existencia " +
                            "actual, debes indicar CostoUnitarioEntrada."
                    });
                }

                var loteNuevo = new LoteInventario
                {
                    ArticuloInventarioId = articulo.ArticuloInventarioId,
                    CompraDetalleId = null,

                    CodigoLote = string.IsNullOrWhiteSpace(request.CodigoLote)
                        ? null
                        : request.CodigoLote.Trim(),

                    FechaVencimiento = request.FechaVencimiento,

                    CantidadInicial = diferencia,
                    CantidadActual = diferencia,

                    CostoUnitario = request.CostoUnitarioEntrada.Value,
                    Activo = true
                };

                _context.LotesInventario.Add(loteNuevo);

                await _context.SaveChangesAsync();

                _context.MovimientosInventario.Add(
                    new MovimientoInventario
                    {
                        ArticuloInventarioId =
                            articulo.ArticuloInventarioId,

                        LoteInventarioId =
                            loteNuevo.LoteInventarioId,

                        TipoMovimiento = "Ajuste",
                        Direccion = "E",
                        Cantidad = diferencia,

                        CostoUnitario =
                            request.CostoUnitarioEntrada.Value,

                        UsuarioId = usuarioId,
                        ReferenciaTipo = "AjusteInventario",
                        ReferenciaId = null,

                        Observaciones =
                            $"Ajuste de entrada. Motivo: " +
                            $"{request.Motivo.Trim()}. " +
                            $"{request.Observaciones}".Trim()
                    });

                await _context.SaveChangesAsync();
                await transaccion.CommitAsync();

                return Ok(new
                {
                    mensaje = "Ajuste de entrada registrado correctamente.",
                    existenciaAnterior = existenciaActual,
                    existenciaNueva = request.CantidadContada,
                    diferencia,
                    lotesAfectados = 1
                });
            }
            catch
            {
                await transaccion.RollbackAsync();
                throw;
            }
        }

        private async Task<(
    bool Exito,
    string? Error,
    int LotesAfectados)> RegistrarSalidaPorLotesAsync(
        int articuloId,
        decimal cantidad,
        string tipoMovimiento,
        string referenciaTipo,
        int usuarioId,
        string observaciones,
        long? loteId,
        bool soloVencidos)
        {
            var consulta = _context.LotesInventario
                .Where(l =>
                    l.ArticuloInventarioId == articuloId &&
                    l.Activo &&
                    l.CantidadActual > 0);

            if (soloVencidos)
            {
                var hoy = DateOnly.FromDateTime(DateTime.Today);

                consulta = consulta.Where(l =>
                    l.FechaVencimiento.HasValue &&
                    l.FechaVencimiento.Value < hoy);
            }

            List<LoteInventario> lotes;

            if (loteId.HasValue)
            {
                var lote = await consulta.FirstOrDefaultAsync(l =>
                    l.LoteInventarioId == loteId.Value);

                if (lote is null)
                {
                    return (
                        false,
                        soloVencidos
                            ? "El lote no existe, no tiene existencias " +
                              "o todavía no está vencido."
                            : "El lote no existe, no pertenece al artículo " +
                              "o no tiene existencias.",
                        0);
                }

                if (lote.CantidadActual < cantidad)
                {
                    return (
                        false,
                        "La cantidad solicitada supera la existencia disponible del lote.",
                        0);
                }

                lotes = [lote];
            }
            else
            {
                lotes = await consulta
                    .OrderBy(l => l.FechaVencimiento.HasValue ? 0 : 1)
                    .ThenBy(l => l.FechaVencimiento)
                    .ThenBy(l => l.FechaIngreso)
                    .ThenBy(l => l.LoteInventarioId)
                    .ToListAsync();

                var cantidadDisponible = lotes.Sum(l => l.CantidadActual);

                if (cantidadDisponible < cantidad)
                {
                    return (
                        false,
                        soloVencidos
                            ? "No hay suficiente cantidad vencida para registrar esta merma."
                            : "No hay existencias suficientes para realizar la operación.",
                        0);
                }
            }

            var pendiente = cantidad;
            var lotesAfectados = 0;

            foreach (var lote in lotes)
            {
                if (pendiente <= 0)
                    break;

                var cantidadRetirada =
                    Math.Min(lote.CantidadActual, pendiente);

                lote.CantidadActual -= cantidadRetirada;
                pendiente -= cantidadRetirada;
                lotesAfectados++;

                _context.MovimientosInventario.Add(
                    new MovimientoInventario
                    {
                        ArticuloInventarioId = articuloId,

                        LoteInventarioId = lote.LoteInventarioId,

                        TipoMovimiento = tipoMovimiento,
                        Direccion = "S",

                        Cantidad = cantidadRetirada,

                        CostoUnitario = lote.CostoUnitario,

                        UsuarioId = usuarioId,

                        ReferenciaTipo = referenciaTipo,
                        ReferenciaId = null,

                        Observaciones = observaciones.Length <= 300
                            ? observaciones
                            : observaciones[..300]
                    });
            }

            return (true, null, lotesAfectados);
        }
    }
}
