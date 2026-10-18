using Heladeria.Api.Data;
using Heladeria.Api.Entities;
using Heladeria.Contracts.Compras;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Api.Controllers
{
    [ApiController]
    [Route("api/compras")]
    [Authorize(Roles = "Administrador,Encargado")]
    public class ComprasController : ControllerBase
    {
        private readonly HeladeriaDbContext _context;

        public ComprasController(HeladeriaDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerCompras(
            [FromQuery] string? estado,
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

            var consulta = _context.Compras
                .AsNoTracking()
                .Include(c => c.Proveedor)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.ArticuloInventario)
                        .ThenInclude(a => a.UnidadMedida)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(estado))
            {
                if (estado is not "Borrador"
                    and not "Recibida"
                    and not "Anulada")
                {
                    return BadRequest(new
                    {
                        mensaje = "El estado indicado no es válido."
                    });
                }

                consulta = consulta.Where(c => c.Estado == estado);
            }

            if (desde.HasValue)
            {
                var fechaInicio = desde.Value.Date;
                consulta = consulta.Where(c => c.FechaCompra >= fechaInicio);
            }

            if (hasta.HasValue)
            {
                var fechaFinalExclusiva = hasta.Value.Date.AddDays(1);

                consulta = consulta.Where(c =>
                    c.FechaCompra < fechaFinalExclusiva);
            }

            var compras = await consulta
                .OrderByDescending(c => c.FechaCompra)
                .ThenByDescending(c => c.CompraId)
                .Take(200)
                .ToListAsync();

            return Ok(compras.Select(ConvertirRespuesta));
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> ObtenerPorId(long id)
        {
            var compra = await ObtenerCompraCompleta(id);

            if (compra is null)
            {
                return NotFound(new
                {
                    mensaje = "La compra no existe."
                });
            }

            return Ok(ConvertirRespuesta(compra));
        }

        [HttpPost]
        public async Task<IActionResult> Crear(
            [FromBody] CrearCompraRequest request)
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

            var numeroCompra = request.NumeroCompra.Trim();

            if (request.Detalles.Count == 0 ||
                request.Detalles.Count > 100)
            {
                return BadRequest(new
                {
                    mensaje = "La compra debe contener entre 1 y 100 detalles."
                });
            }

            if (!TieneMaxDecimales(request.Descuento, 2))
            {
                return BadRequest(new
                {
                    mensaje = "El descuento general admite como máximo dos decimales."
                });
            }

            var proveedor = await _context.Proveedores
                .FirstOrDefaultAsync(p =>
                    p.ProveedorId == request.ProveedorId &&
                    p.Activo);

            if (proveedor is null)
            {
                return BadRequest(new
                {
                    mensaje = "El proveedor no existe o está inactivo."
                });
            }

            var numeroExiste = await _context.Compras
                .AnyAsync(c => c.NumeroCompra == numeroCompra);

            if (numeroExiste)
            {
                return Conflict(new
                {
                    mensaje = "Ese número de compra ya está registrado."
                });
            }

            var articuloIds = request.Detalles
                .Select(d => d.ArticuloInventarioId)
                .Distinct()
                .ToList();

            var articulosActivos = await _context.ArticulosInventario
                .Where(a =>
                    articuloIds.Contains(a.ArticuloInventarioId) &&
                    a.Activo)
                .Select(a => a.ArticuloInventarioId)
                .ToListAsync();

            if (articulosActivos.Count != articuloIds.Count)
            {
                return BadRequest(new
                {
                    mensaje =
                        "Uno o más artículos no existen o están inactivos."
                });
            }

            decimal subtotal = 0;
            decimal descuentosDetalles = 0;

            var detallesCalculados =
                new List<(CrearCompraDetalleRequest Request,
                          decimal Bruto,
                          decimal Importe)>();

            foreach (var detalle in request.Detalles)
            {
                if (detalle.Cantidad <= 0 ||
                    !TieneMaxDecimales(detalle.Cantidad, 3))
                {
                    return BadRequest(new
                    {
                        mensaje =
                            "Las cantidades deben ser positivas y admitir " +
                            "como máximo tres decimales."
                    });
                }

                if (detalle.CostoUnitario < 0 ||
                    !TieneMaxDecimales(detalle.CostoUnitario, 4))
                {
                    return BadRequest(new
                    {
                        mensaje =
                            "El costo unitario no puede ser negativo y " +
                            "admite como máximo cuatro decimales."
                    });
                }

                if (detalle.Descuento < 0 ||
                    !TieneMaxDecimales(detalle.Descuento, 2))
                {
                    return BadRequest(new
                    {
                        mensaje =
                            "El descuento por línea admite como máximo " +
                            "dos decimales y no puede ser negativo."
                    });
                }

                var bruto = Math.Round(
                    detalle.Cantidad * detalle.CostoUnitario,
                    2,
                    MidpointRounding.AwayFromZero);

                if (detalle.Descuento > bruto)
                {
                    return BadRequest(new
                    {
                        mensaje =
                            "El descuento de una línea no puede superar " +
                            "su importe bruto."
                    });
                }

                var importe = bruto - detalle.Descuento;

                subtotal += bruto;
                descuentosDetalles += detalle.Descuento;

                detallesCalculados.Add((detalle, bruto, importe));
            }

            subtotal = Math.Round(subtotal, 2);

            if (request.Descuento >
                subtotal - descuentosDetalles)
            {
                return BadRequest(new
                {
                    mensaje =
                        "El descuento general supera el importe disponible."
                });
            }

            var descuentoTotal =
                descuentosDetalles + request.Descuento;

            var total = subtotal - descuentoTotal;

            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var compra = new Compra
                {
                    NumeroCompra = numeroCompra,
                    ProveedorId = proveedor.ProveedorId,
                    UsuarioId = usuarioId,
                    Estado = "Borrador",
                    Subtotal = subtotal,
                    Descuento = descuentoTotal,
                    Total = total,
                    Observaciones = string.IsNullOrWhiteSpace(request.Observaciones)
                        ? null
                        : request.Observaciones.Trim()
                };

                foreach (var calculado in detallesCalculados)
                {
                    compra.Detalles.Add(new CompraDetalle
                    {
                        ArticuloInventarioId =
                            calculado.Request.ArticuloInventarioId,

                        Cantidad = calculado.Request.Cantidad,

                        CostoUnitario =
                            calculado.Request.CostoUnitario,

                        Descuento = calculado.Request.Descuento,

                        Importe = calculado.Importe
                    });
                }

                _context.Compras.Add(compra);

                await _context.SaveChangesAsync();

                await transaccion.CommitAsync();

                var compraCompleta = await ObtenerCompraCompleta(
                    compra.CompraId);

                return CreatedAtAction(
                    nameof(ObtenerPorId),
                    new { id = compra.CompraId },
                    ConvertirRespuesta(compraCompleta!));
            }
            catch
            {
                await transaccion.RollbackAsync();
                throw;
            }
        }

        // POST: api/compras/1/recibir
        [HttpPost("{id:long}/recibir")]
        public async Task<IActionResult> Recibir(
            long id,
            [FromBody] RecepcionarCompraRequest request)
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

            if (request.Detalles.Count == 0 ||
                request.Detalles.Count > 100)
            {
                return BadRequest(new
                {
                    mensaje = "La recepción debe contener entre 1 y 100 líneas."
                });
            }

            var idsSolicitados = request.Detalles
                .Select(d => d.CompraDetalleId)
                .ToList();

            if (idsSolicitados.Distinct().Count() != idsSolicitados.Count)
            {
                return BadRequest(new
                {
                    mensaje = "No puedes repetir una línea de compra."
                });
            }

            var hoy = DateOnly.FromDateTime(DateTime.Today);

            foreach (var detalle in request.Detalles)
            {
                if (detalle.FechaVencimiento.HasValue &&
                    detalle.FechaVencimiento.Value < hoy)
                {
                    return BadRequest(new
                    {
                        mensaje =
                            "No puedes recibir lotes cuya fecha de vencimiento ya pasó."
                    });
                }
            }

            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var compra = await _context.Compras
                    .Include(c => c.Detalles)
                    .FirstOrDefaultAsync(c => c.CompraId == id);

                if (compra is null)
                {
                    return NotFound(new
                    {
                        mensaje = "La compra no existe."
                    });
                }

                if (compra.Estado != "Borrador")
                {
                    return Conflict(new
                    {
                        mensaje =
                            "Solo se pueden recibir compras que estén en borrador."
                    });
                }

                var idsCompra = compra.Detalles
                    .Select(d => d.CompraDetalleId)
                    .OrderBy(x => x)
                    .ToList();

                if (!idsCompra.SequenceEqual(
                        idsSolicitados.OrderBy(x => x)))
                {
                    return BadRequest(new
                    {
                        mensaje =
                            "Debes enviar exactamente todas las líneas de esta compra."
                    });
                }

                // Cambio condicional: impide que la misma compra
                // se reciba dos veces por solicitudes simultáneas.
                var filasActualizadas = await _context.Compras
                    .Where(c =>
                        c.CompraId == id &&
                        c.Estado == "Borrador")
                    .ExecuteUpdateAsync(setters =>
                        setters.SetProperty(
                            c => c.Estado,
                            "Recibida"));

                if (filasActualizadas != 1)
                {
                    return Conflict(new
                    {
                        mensaje =
                            "La compra ya fue recibida o su estado ha cambiado."
                    });
                }

                compra.Estado = "Recibida";

                var datosRecepcion = request.Detalles
                    .ToDictionary(d => d.CompraDetalleId);

                var lotesPorDetalle =
                    new Dictionary<long, LoteInventario>();

                foreach (var detalleCompra in compra.Detalles)
                {
                    var datos = datosRecepcion[
                        detalleCompra.CompraDetalleId];

                    var lote = new LoteInventario
                    {
                        ArticuloInventarioId =
                            detalleCompra.ArticuloInventarioId,

                        CompraDetalleId =
                            detalleCompra.CompraDetalleId,

                        CodigoLote = string.IsNullOrWhiteSpace(
                            datos.CodigoLote)
                            ? null
                            : datos.CodigoLote.Trim(),

                        FechaVencimiento = datos.FechaVencimiento,

                        CantidadInicial = detalleCompra.Cantidad,

                        CantidadActual = detalleCompra.Cantidad,

                        CostoUnitario = detalleCompra.CostoUnitario,

                        Activo = true
                    };

                    _context.LotesInventario.Add(lote);

                    lotesPorDetalle.Add(
                        detalleCompra.CompraDetalleId,
                        lote);
                }

                // Guardar los lotes para obtener sus identificadores.
                // Todo sigue dentro de la misma transacción.
                await _context.SaveChangesAsync();

                foreach (var detalleCompra in compra.Detalles)
                {
                    var lote = lotesPorDetalle[
                        detalleCompra.CompraDetalleId];

                    var movimiento = new MovimientoInventario
                    {
                        ArticuloInventarioId =
                            detalleCompra.ArticuloInventarioId,

                        LoteInventarioId = lote.LoteInventarioId,

                        TipoMovimiento = "Compra",

                        Direccion = "E",

                        Cantidad = detalleCompra.Cantidad,

                        CostoUnitario = detalleCompra.CostoUnitario,

                        UsuarioId = usuarioId,

                        ReferenciaTipo = "Compra",

                        ReferenciaId = compra.CompraId,

                        Observaciones =
                            $"Recepción de compra {compra.NumeroCompra}"
                    };

                    _context.MovimientosInventario.Add(movimiento);
                }

                await _context.SaveChangesAsync();

                await transaccion.CommitAsync();

                return Ok(new
                {
                    mensaje = "Compra recibida correctamente.",
                    compraId = compra.CompraId,
                    numeroCompra = compra.NumeroCompra,
                    estado = compra.Estado,
                    lotesCreados = lotesPorDetalle.Count,
                    movimientosCreados = compra.Detalles.Count
                });
            }
            catch
            {
                await transaccion.RollbackAsync();
                throw;
            }
        }

        private Task<Compra?> ObtenerCompraCompleta(long id)
        {
            return _context.Compras
                .AsNoTracking()
                .Include(c => c.Proveedor)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.ArticuloInventario)
                        .ThenInclude(a => a.UnidadMedida)
                .FirstOrDefaultAsync(c => c.CompraId == id);
        }

        private static bool TieneMaxDecimales(
            decimal valor,
            int decimales)
        {
            return valor == decimal.Round(valor, decimales);
        }

        private static CompraResponse ConvertirRespuesta(Compra compra)
        {
            return new CompraResponse
            {
                CompraId = compra.CompraId,
                NumeroCompra = compra.NumeroCompra,
                ProveedorId = compra.ProveedorId,
                ProveedorNombre = compra.Proveedor.Nombre,
                UsuarioId = compra.UsuarioId,
                FechaCompra = compra.FechaCompra,
                Estado = compra.Estado,
                Subtotal = compra.Subtotal,
                Descuento = compra.Descuento,
                Total = compra.Total,
                Observaciones = compra.Observaciones,

                Detalles = compra.Detalles
                    .OrderBy(d => d.CompraDetalleId)
                    .Select(d => new CompraDetalleResponse
                    {
                        CompraDetalleId = d.CompraDetalleId,
                        ArticuloInventarioId = d.ArticuloInventarioId,
                        NombreArticulo = d.ArticuloInventario.Nombre,
                        UnidadMedida = d.ArticuloInventario.UnidadMedida.Nombre,
                        Abreviatura = d.ArticuloInventario.UnidadMedida.Abreviatura,
                        Cantidad = d.Cantidad,
                        CostoUnitario = d.CostoUnitario,
                        Descuento = d.Descuento,
                        Importe = d.Importe
                    })
                    .ToList()
            };
        }
    }
}
