using Heladeria.Api.Data;
using Heladeria.Api.Entities;
using Heladeria.Contracts.Ventas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Heladeria.Api.Controllers
{
    [ApiController]
    [Route("api/ventas")]
    [Authorize(Roles = "Administrador,Cajero,Encargado")]
    public class VentasController : ControllerBase
    {
        private readonly HeladeriaDbContext _context;

        public VentasController(HeladeriaDbContext context)
        {
            _context = context;
        }

        // GET: api/ventas
        [HttpGet]
        public async Task<IActionResult> ObtenerVentas(
            [FromQuery] DateTime? desde,
            [FromQuery] DateTime? hasta)
        {
            if (desde.HasValue && hasta.HasValue &&
                desde.Value.Date > hasta.Value.Date)
            {
                return BadRequest(new
                {
                    mensaje = "La fecha inicial no puede superar la fecha final."
                });
            }

            var consulta = _context.Ventas
                .AsNoTracking()
                .Include(v => v.Usuario)
                .Include(v => v.Caja)
                .Include(v => v.Detalles)
                .Include(v => v.Pagos)
                .AsQueryable();

            // Los cajeros solo consultan sus propias ventas.
            if (!User.IsInRole("Administrador") &&
                !User.IsInRole("Encargado"))
            {
                var usuarioTexto =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(usuarioTexto, out var usuarioId))
                    return Unauthorized();

                consulta = consulta.Where(v => v.UsuarioId == usuarioId);
            }

            if (desde.HasValue)
            {
                var inicio = desde.Value.Date;
                consulta = consulta.Where(v => v.FechaVenta >= inicio);
            }

            if (hasta.HasValue)
            {
                var fin = hasta.Value.Date.AddDays(1);
                consulta = consulta.Where(v => v.FechaVenta < fin);
            }

            var ventas = await consulta
                .OrderByDescending(v => v.FechaVenta)
                .Take(200)
                .ToListAsync();

            return Ok(ventas.Select(ConvertirRespuesta));
        }

        // GET: api/ventas/1
        [HttpGet("{id:long}")]
        public async Task<IActionResult> ObtenerPorId(long id)
        {
            var consulta = _context.Ventas
                .AsNoTracking()
                .Include(v => v.Usuario)
                .Include(v => v.Caja)
                .Include(v => v.Detalles)
                .Include(v => v.Pagos)
                .Where(v => v.VentaId == id);

            if (!User.IsInRole("Administrador") &&
                !User.IsInRole("Encargado"))
            {
                var usuarioTexto =
                    User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(usuarioTexto, out var usuarioId))
                    return Unauthorized();

                consulta = consulta.Where(v => v.UsuarioId == usuarioId);
            }

            var venta = await consulta.FirstOrDefaultAsync();

            if (venta is null)
            {
                return NotFound(new
                {
                    mensaje = "La venta no existe o no tienes permiso para consultarla."
                });
            }

            return Ok(ConvertirRespuesta(venta));
        }

        // POST: api/ventas
        [HttpPost]
        public async Task<IActionResult> Crear(
            [FromBody] CrearVentaRequest request)
        {
            var usuarioTexto =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(usuarioTexto, out var usuarioId))
                return Unauthorized();

            if (request.Detalles.Count == 0 ||
                request.Detalles.Count > 100)
            {
                return BadRequest(new
                {
                    mensaje = "La venta debe contener entre 1 y 100 líneas."
                });
            }

            var metodoPago = request.MetodoPago.Trim();

            if (metodoPago is not "Efectivo" and not "Transferencia")
            {
                return BadRequest(new
                {
                    mensaje = "El método de pago debe ser Efectivo o Transferencia."
                });
            }

            if (metodoPago == "Efectivo")
            {
                if (!request.MontoRecibido.HasValue ||
                    request.MontoRecibido.Value <= 0 ||
                    request.MontoRecibido.Value !=
                        decimal.Round(request.MontoRecibido.Value, 2))
                {
                    return BadRequest(new
                    {
                        mensaje = "Para efectivo, introduce el importe recibido con hasta dos decimales."
                    });
                }
            }
            else if (request.MontoRecibido.HasValue)
            {
                return BadRequest(new
                {
                    mensaje = "MontoRecibido debe ser null para transferencias."
                });
            }

            foreach (var detalle in request.Detalles)
            {
                if (detalle.Cantidad <= 0 ||
                    detalle.Cantidad != decimal.Round(detalle.Cantidad, 3))
                {
                    return BadRequest(new
                    {
                        mensaje = "Las cantidades deben ser positivas y admitir hasta tres decimales."
                    });
                }
            }

            // Consolidar presentaciones repetidas.
            var lineas = request.Detalles
                .GroupBy(d => d.ProductoPresentacionId)
                .Select(g => new
                {
                    ProductoPresentacionId = g.Key,
                    Cantidad = g.Sum(x => x.Cantidad)
                })
                .ToList();

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                var usuario = await _context.Usuarios
                    .FirstOrDefaultAsync(u =>
                        u.UsuarioId == usuarioId && u.Activo);

                if (usuario is null)
                {
                    await transaction.RollbackAsync();
                    return Unauthorized(new
                    {
                        mensaje = "El usuario no existe o está inactivo."
                    });
                }

                var caja = await _context.Cajas
                    .FirstOrDefaultAsync(c =>
                        c.CajaId == request.CajaId && c.Activa);

                if (caja is null)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new
                    {
                        mensaje = "La caja no existe o está inactiva."
                    });
                }

                var idsPresentaciones = lineas
                    .Select(l => l.ProductoPresentacionId)
                    .ToList();

                var presentaciones = await _context.ProductoPresentaciones
                    .Include(p => p.Producto)
                        .ThenInclude(p => p.Categoria)
                    .Where(p =>
                        idsPresentaciones.Contains(p.ProductoPresentacionId) &&
                        p.Activa &&
                        p.Producto.Activo &&
                        p.Producto.Categoria.Activa)
                    .ToListAsync();

                if (presentaciones.Count != idsPresentaciones.Count)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new
                    {
                        mensaje = "Una o más presentaciones no existen o están inactivas."
                    });
                }

                var porId = presentaciones.ToDictionary(
                    p => p.ProductoPresentacionId);

                decimal subtotal = 0;

                foreach (var linea in lineas)
                {
                    var presentacion = porId[linea.ProductoPresentacionId];

                    subtotal += Math.Round(
                        linea.Cantidad * presentacion.PrecioVenta,
                        2,
                        MidpointRounding.AwayFromZero);
                }

                subtotal = Math.Round(subtotal, 2);

                if (subtotal <= 0)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new
                    {
                        mensaje = "El total de la venta debe ser mayor que cero."
                    });
                }

                if (metodoPago == "Efectivo" &&
                    request.MontoRecibido!.Value < subtotal)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new
                    {
                        mensaje = "El importe recibido no cubre el total de la venta."
                    });
                }

                var numeroVenta =
                    $"V-{DateTime.Now:yyyyMMddHHmmss}-" +
                    Guid.NewGuid().ToString("N")[..6];

                var venta = new Venta
                {
                    NumeroVenta = numeroVenta,
                    FechaVenta = DateTime.Now,
                    UsuarioId = usuarioId,
                    Usuario = usuario,
                    CajaId = caja.CajaId,
                    Caja = caja,
                    Estado = "Completada",
                    Subtotal = subtotal,
                    Descuento = 0,
                    Total = subtotal,
                    Observaciones = string.IsNullOrWhiteSpace(request.Observaciones)
                        ? null
                        : request.Observaciones.Trim()
                };

                _context.Ventas.Add(venta);

                // Obtener el ID de la venta dentro de la transacción.
                await _context.SaveChangesAsync();

                decimal costoTotalVenta = 0;

                foreach (var linea in lineas)
                {
                    var presentacion = porId[linea.ProductoPresentacionId];

                    decimal costoLinea = 0;

                    // Si existe un artículo de producto terminado vinculado,
                    // descontamos la presentación directamente del inventario.
                    var articuloTerminado = await _context.ArticulosInventario
                        .FirstOrDefaultAsync(a =>
                            a.ProductoPresentacionId ==
                                presentacion.ProductoPresentacionId &&
                            a.TipoArticulo == "ProductoTerminado" &&
                            a.Activo);

                    if (articuloTerminado is not null)
                    {
                        var consumo = await ConsumirDeLotesAsync(
                            articuloTerminado.ArticuloInventarioId,
                            linea.Cantidad,
                            usuarioId,
                            venta.VentaId,
                            numeroVenta);

                        if (!consumo.Exito)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest(new { mensaje = consumo.Error });
                        }

                        costoLinea += consumo.CostoTotal;
                    }
                    else
                    {
                        var receta = await _context.RecetaDetalles
                            .Include(r => r.ArticuloInventario)
                            .Where(r =>
                                r.ProductoPresentacionId ==
                                    presentacion.ProductoPresentacionId)
                            .ToListAsync();

                        if (receta.Count == 0)
                        {
                            await transaction.RollbackAsync();
                            return BadRequest(new
                            {
                                mensaje =
                                    $"La presentación '{presentacion.Producto.Nombre} - " +
                                    $"{presentacion.NombrePresentacion}' no tiene receta " +
                                    "ni artículo de producto terminado vinculado al inventario."
                            });
                        }

                        foreach (var ingrediente in receta)
                        {
                            if (!ingrediente.ArticuloInventario.Activo)
                            {
                                await transaction.RollbackAsync();
                                return BadRequest(new
                                {
                                    mensaje =
                                        $"El artículo '{ingrediente.ArticuloInventario.Nombre}' " +
                                        "de la receta está inactivo."
                                });
                            }

                            // Los lotes admiten tres decimales de cantidad.
                            // Redondeamos hacia arriba para no consumir menos
                            // inventario del que requiere la receta.
                            var cantidadNecesaria = decimal.Ceiling(
                                linea.Cantidad *
                                ingrediente.CantidadPorUnidad * 1000m) / 1000m;

                            var consumo = await ConsumirDeLotesAsync(
                                ingrediente.ArticuloInventarioId,
                                cantidadNecesaria,
                                usuarioId,
                                venta.VentaId,
                                numeroVenta);

                            if (!consumo.Exito)
                            {
                                await transaction.RollbackAsync();
                                return BadRequest(new
                                {
                                    mensaje =
                                        $"Inventario insuficiente para " +
                                        $"'{ingrediente.ArticuloInventario.Nombre}': " +
                                        consumo.Error
                                });
                            }

                            costoLinea += consumo.CostoTotal;
                        }
                    }

                    var importeLinea = Math.Round(
                        linea.Cantidad * presentacion.PrecioVenta,
                        2,
                        MidpointRounding.AwayFromZero);

                    var detalleVenta = new DetalleVenta
                    {
                        VentaId = venta.VentaId,
                        ProductoPresentacionId =
                            presentacion.ProductoPresentacionId,

                        NombreProducto = presentacion.Producto.Nombre,
                        NombrePresentacion = presentacion.NombrePresentacion,
                        CodigoInterno = presentacion.CodigoInterno,

                        Cantidad = linea.Cantidad,
                        PrecioUnitario = presentacion.PrecioVenta,

                        CostoUnitario = Math.Round(
                            costoLinea / linea.Cantidad,
                            4,
                            MidpointRounding.AwayFromZero),

                        Descuento = 0,
                        ImporteLinea = importeLinea
                    };

                    _context.DetalleVentas.Add(detalleVenta);
                    costoTotalVenta += costoLinea;
                }

                var pago = new Pago
                {
                    VentaId = venta.VentaId,
                    MetodoPago = metodoPago,
                    Importe = venta.Total,
                    Referencia = string.IsNullOrWhiteSpace(request.Referencia)
                        ? null
                        : request.Referencia.Trim(),
                    FechaPago = DateTime.Now,
                    MontoRecibido = metodoPago == "Efectivo"
                        ? request.MontoRecibido
                        : null,
                    Cambio = metodoPago == "Efectivo"
                        ? request.MontoRecibido!.Value - venta.Total
                        : 0
                };

                _context.Pagos.Add(pago);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(
                    nameof(ObtenerPorId),
                    new { id = venta.VentaId },
                    new
                    {
                        ventaId = venta.VentaId,
                        numeroVenta = venta.NumeroVenta,
                        fechaVenta = venta.FechaVenta,
                        usuario = usuario.NombreUsuario,
                        caja = caja.Nombre,
                        estado = venta.Estado,
                        subtotal = venta.Subtotal,
                        total = venta.Total,
                        metodoPago = pago.MetodoPago,
                        montoRecibido = pago.MontoRecibido,
                        cambio = pago.Cambio,
                        costoTotalEstimado = Math.Round(costoTotalVenta, 2)
                    });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task<(
            bool Exito,
            string? Error,
            decimal CostoTotal)> ConsumirDeLotesAsync(
                int articuloInventarioId,
                decimal cantidad,
                int usuarioId,
                long ventaId,
                string numeroVenta)
        {
            var hoy = DateOnly.FromDateTime(DateTime.Today);

            // Los lotes vencidos quedan excluidos de las ventas.
            var lotes = await _context.LotesInventario
                .Where(l =>
                    l.ArticuloInventarioId == articuloInventarioId &&
                    l.Activo &&
                    l.CantidadActual > 0 &&
                    (!l.FechaVencimiento.HasValue ||
                     l.FechaVencimiento.Value >= hoy))
                .OrderBy(l => l.FechaVencimiento.HasValue ? 0 : 1)
                .ThenBy(l => l.FechaVencimiento)
                .ThenBy(l => l.FechaIngreso)
                .ThenBy(l => l.LoteInventarioId)
                .ToListAsync();

            var disponible = lotes.Sum(l => l.CantidadActual);

            if (disponible < cantidad)
            {
                return (
                    false,
                    $"Disponible: {disponible}; requerido: {cantidad}.",
                    0m);
            }

            var pendiente = cantidad;
            decimal costoTotal = 0;

            foreach (var lote in lotes)
            {
                if (pendiente <= 0)
                    break;

                var cantidadRetirada = Math.Min(
                    lote.CantidadActual,
                    pendiente);

                lote.CantidadActual -= cantidadRetirada;
                pendiente -= cantidadRetirada;

                costoTotal += cantidadRetirada * lote.CostoUnitario;

                _context.MovimientosInventario.Add(
                    new MovimientoInventario
                    {
                        ArticuloInventarioId = articuloInventarioId,
                        LoteInventarioId = lote.LoteInventarioId,
                        TipoMovimiento = "Venta",
                        Direccion = "S",
                        Cantidad = cantidadRetirada,
                        CostoUnitario = lote.CostoUnitario,
                        FechaMovimiento = DateTime.Now,
                        UsuarioId = usuarioId,
                        ReferenciaTipo = "Venta",
                        ReferenciaId = ventaId,
                        Observaciones = $"Venta {numeroVenta}"
                    });
            }

            return (true, null, costoTotal);
        }

        private static VentaResponse ConvertirRespuesta(Venta venta)
        {
            return new VentaResponse
            {
                VentaId = venta.VentaId,
                NumeroVenta = venta.NumeroVenta,
                FechaVenta = venta.FechaVenta,
                UsuarioId = venta.UsuarioId,
                NombreUsuario = venta.Usuario.NombreUsuario,
                CajaId = venta.CajaId,
                NombreCaja = venta.Caja.Nombre,
                Estado = venta.Estado,
                Subtotal = venta.Subtotal,
                Descuento = venta.Descuento,
                Total = venta.Total,
                Observaciones = venta.Observaciones,

                Detalles = venta.Detalles
                    .OrderBy(d => d.DetalleVentaId)
                    .Select(d => new VentaDetalleResponse
                    {
                        DetalleVentaId = d.DetalleVentaId,
                        ProductoPresentacionId = d.ProductoPresentacionId,
                        NombreProducto = d.NombreProducto,
                        NombrePresentacion = d.NombrePresentacion,
                        CodigoInterno = d.CodigoInterno,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario,
                        CostoUnitario = d.CostoUnitario,
                        Descuento = d.Descuento,
                        ImporteLinea = d.ImporteLinea
                    })
                    .ToList(),

                Pagos = venta.Pagos
                    .OrderBy(p => p.PagoId)
                    .Select(p => new PagoResponse
                    {
                        PagoId = p.PagoId,
                        MetodoPago = p.MetodoPago,
                        Importe = p.Importe,
                        MontoRecibido = p.MontoRecibido,
                        Cambio = p.Cambio,
                        Referencia = p.Referencia,
                        FechaPago = p.FechaPago
                    })
                    .ToList()
            };
        }
    }
}
