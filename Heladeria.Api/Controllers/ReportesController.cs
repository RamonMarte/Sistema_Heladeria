using Heladeria.Api.Data;
using Heladeria.Api.Entities;
using Heladeria.Contracts.Reportes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Api.Controllers
{
    [ApiController]
    [Route("api/reportes")]
    [Authorize(Roles = "Administrador,Encargado")]
    public class ReportesController : ControllerBase
    {
        private readonly HeladeriaDbContext _context;

        public ReportesController(HeladeriaDbContext context)
        {
            _context = context;
        }

        // GET: api/reportes/ventas?periodo=hoy
        [HttpGet("ventas")]
        public async Task<IActionResult> ObtenerResumenVentas(
            [FromQuery] string periodo = "hoy",
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null,
            [FromQuery] int? usuarioId = null,
            [FromQuery] int? productoPresentacionId = null,
            [FromQuery] int? cajaId = null)
        {
            if (!IntentarResolverRango(
                    periodo,
                    desde,
                    hasta,
                    out var fechaInicio,
                    out var fechaFinExclusiva,
                    out var periodoNormalizado,
                    out var error))
            {
                return BadRequest(new { mensaje = error });
            }

            var validacion = await ValidarFiltrosAsync(
                usuarioId,
                productoPresentacionId,
                cajaId);

            if (validacion is not null)
                return BadRequest(new { mensaje = validacion });

            var ventas = CrearConsultaVentas(
                fechaInicio,
                fechaFinExclusiva,
                usuarioId,
                productoPresentacionId,
                cajaId);

            var idsVentas = ventas.Select(v => v.VentaId);

            var cantidadTransacciones = await ventas.CountAsync();

            var importeTotalTickets =
                await ventas.SumAsync(v => (decimal?)v.Total) ?? 0m;

            var descuentosTickets =
                await ventas.SumAsync(v => (decimal?)v.Descuento) ?? 0m;

            // Si se seleccionó una presentación, estas métricas se limitan
            // a las líneas de esa presentación dentro de los tickets incluidos.
            IQueryable<DetalleVenta> detalles = _context.DetalleVentas
                .AsNoTracking()
                .Where(d => idsVentas.Contains(d.VentaId));

            if (productoPresentacionId.HasValue)
            {
                detalles = detalles.Where(d =>
                    d.ProductoPresentacionId ==
                    productoPresentacionId.Value);
            }

            var unidadesVendidas =
                await detalles.SumAsync(d => (decimal?)d.Cantidad) ?? 0m;

            var importeProductos =
                await detalles.SumAsync(
                    d => (decimal?)(d.ImporteLinea - d.Descuento)) ?? 0m;

            var costoEstimadoProductos =
                await detalles.SumAsync(
                    d => (decimal?)(d.Cantidad * d.CostoUnitario)) ?? 0m;

            var gananciaBrutaEstimada =
                importeProductos - costoEstimadoProductos;

            var pagosPorMetodo = await _context.Pagos
                .AsNoTracking()
                .Where(p => idsVentas.Contains(p.VentaId))
                .GroupBy(p => p.MetodoPago)
                .Select(g => new PagoMetodoReporteResponse
                {
                    MetodoPago = g.Key,
                    CantidadPagos = g.LongCount(),
                    Importe = g.Sum(p => p.Importe)
                })
                .OrderByDescending(p => p.Importe)
                .ToListAsync();

            var productosMasVendidos = await detalles
                .GroupBy(d => new
                {
                    d.ProductoPresentacionId,
                    d.NombreProducto,
                    d.NombrePresentacion
                })
                .Select(g => new ProductoVendidoReporteResponse
                {
                    ProductoPresentacionId =
                        g.Key.ProductoPresentacionId,

                    NombreProducto = g.Key.NombreProducto,

                    NombrePresentacion = g.Key.NombrePresentacion,

                    CantidadVendida = g.Sum(d => d.Cantidad),

                    Importe = g.Sum(d => d.ImporteLinea - d.Descuento),

                    CostoEstimado = g.Sum(
                        d => d.Cantidad * d.CostoUnitario),

                    GananciaBrutaEstimada = g.Sum(
                        d => (d.ImporteLinea - d.Descuento) -
                             (d.Cantidad * d.CostoUnitario))
                })
                .OrderByDescending(p => p.CantidadVendida)
                .Take(10)
                .ToListAsync();

            var ventasPorDia = await ventas
                .GroupBy(v => v.FechaVenta.Date)
                .Select(g => new VentaDiaReporteResponse
                {
                    Fecha = g.Key,
                    CantidadTransacciones = g.LongCount(),
                    ImporteTotal = g.Sum(v => v.Total)
                })
                .OrderBy(v => v.Fecha)
                .ToListAsync();

            return Ok(new ReporteVentasResponse
            {
                Periodo = periodoNormalizado,
                FechaDesde = fechaInicio,
                FechaHasta = fechaFinExclusiva.AddDays(-1),

                CantidadTransacciones = cantidadTransacciones,
                ImporteTotalTickets = importeTotalTickets,
                DescuentosTickets = descuentosTickets,

                UnidadesVendidas = unidadesVendidas,
                ImporteProductos = importeProductos,
                CostoEstimadoProductos = costoEstimadoProductos,
                GananciaBrutaEstimada = gananciaBrutaEstimada,

                PagosPorMetodo = pagosPorMetodo,
                ProductosMasVendidos = productosMasVendidos,
                VentasPorDia = ventasPorDia
            });
        }

        // GET: api/reportes/ventas-por-empleado?periodo=mes
        [HttpGet("ventas-por-empleado")]
        public async Task<IActionResult> ObtenerVentasPorEmpleado(
            [FromQuery] string periodo = "hoy",
            [FromQuery] DateTime? desde = null,
            [FromQuery] DateTime? hasta = null,
            [FromQuery] int? usuarioId = null,
            [FromQuery] int? productoPresentacionId = null,
            [FromQuery] int? cajaId = null)
        {
            if (!IntentarResolverRango(
                    periodo,
                    desde,
                    hasta,
                    out var fechaInicio,
                    out var fechaFinExclusiva,
                    out _,
                    out var error))
            {
                return BadRequest(new { mensaje = error });
            }

            var validacion = await ValidarFiltrosAsync(
                usuarioId,
                productoPresentacionId,
                cajaId);

            if (validacion is not null)
                return BadRequest(new { mensaje = validacion });

            var ventas = CrearConsultaVentas(
                fechaInicio,
                fechaFinExclusiva,
                usuarioId,
                productoPresentacionId,
                cajaId);

            var resumenTickets = await (
                from v in ventas
                join u in _context.Usuarios.AsNoTracking()
                    on v.UsuarioId equals u.UsuarioId
                group new { v, u } by new
                {
                    v.UsuarioId,
                    u.NombreUsuario,
                    u.NombreCompleto
                }
                into g
                select new
                {
                    g.Key.UsuarioId,
                    g.Key.NombreUsuario,
                    g.Key.NombreCompleto,
                    CantidadTransacciones = g.LongCount(),
                    ImporteTotalTickets = g.Sum(x => x.v.Total)
                })
                .ToListAsync();

            IQueryable<DetalleVenta> detalles = _context.DetalleVentas
                .AsNoTracking();

            if (productoPresentacionId.HasValue)
            {
                detalles = detalles.Where(d =>
                    d.ProductoPresentacionId ==
                    productoPresentacionId.Value);
            }

            var resumenProductos = await (
                from d in detalles
                join v in ventas on d.VentaId equals v.VentaId
                group new { d, v } by v.UsuarioId into g
                select new
                {
                    UsuarioId = g.Key,
                    UnidadesVendidas = g.Sum(x => x.d.Cantidad),
                    ImporteProductos = g.Sum(
                        x => x.d.ImporteLinea - x.d.Descuento),
                    CostoEstimadoProductos = g.Sum(
                        x => x.d.Cantidad * x.d.CostoUnitario)
                })
                .ToListAsync();

            var productosPorUsuario = resumenProductos.ToDictionary(
                r => r.UsuarioId);

            var respuesta = resumenTickets.Select(t =>
            {
                productosPorUsuario.TryGetValue(
                    t.UsuarioId,
                    out var producto);

                var importeProductos =
                    producto?.ImporteProductos ?? 0m;

                var costoProductos =
                    producto?.CostoEstimadoProductos ?? 0m;

                return new VentasEmpleadoReporteResponse
                {
                    UsuarioId = t.UsuarioId,
                    NombreUsuario = t.NombreUsuario,
                    NombreCompleto = t.NombreCompleto,

                    CantidadTransacciones = t.CantidadTransacciones,
                    ImporteTotalTickets = t.ImporteTotalTickets,

                    UnidadesVendidas =
                        producto?.UnidadesVendidas ?? 0m,

                    ImporteProductos = importeProductos,
                    CostoEstimadoProductos = costoProductos,

                    GananciaBrutaEstimada =
                        importeProductos - costoProductos
                };
            })
            .OrderByDescending(r => r.ImporteTotalTickets)
            .ToList();

            return Ok(respuesta);
        }

        private IQueryable<Venta> CrearConsultaVentas(
            DateTime fechaInicio,
            DateTime fechaFinExclusiva,
            int? usuarioId,
            int? productoPresentacionId,
            int? cajaId)
        {
            var consulta = _context.Ventas
                .AsNoTracking()
                .Where(v =>
                    v.Estado == "Completada" &&
                    v.FechaVenta >= fechaInicio &&
                    v.FechaVenta < fechaFinExclusiva);

            if (usuarioId.HasValue)
            {
                consulta = consulta.Where(v =>
                    v.UsuarioId == usuarioId.Value);
            }

            if (cajaId.HasValue)
            {
                consulta = consulta.Where(v =>
                    v.CajaId == cajaId.Value);
            }

            if (productoPresentacionId.HasValue)
            {
                consulta = consulta.Where(v =>
                    v.Detalles.Any(d =>
                        d.ProductoPresentacionId ==
                        productoPresentacionId.Value));
            }

            return consulta;
        }

        private async Task<string?> ValidarFiltrosAsync(
            int? usuarioId,
            int? productoPresentacionId,
            int? cajaId)
        {
            if (usuarioId.HasValue &&
                !await _context.Usuarios.AnyAsync(u =>
                    u.UsuarioId == usuarioId.Value))
            {
                return "El usuario indicado no existe.";
            }

            if (productoPresentacionId.HasValue &&
                !await _context.ProductoPresentaciones.AnyAsync(p =>
                    p.ProductoPresentacionId ==
                    productoPresentacionId.Value))
            {
                return "La presentación indicada no existe.";
            }

            if (cajaId.HasValue &&
                !await _context.Cajas.AnyAsync(c =>
                    c.CajaId == cajaId.Value))
            {
                return "La caja indicada no existe.";
            }

            return null;
        }

        private static bool IntentarResolverRango(
            string periodo,
            DateTime? desde,
            DateTime? hasta,
            out DateTime fechaInicio,
            out DateTime fechaFinExclusiva,
            out string periodoNormalizado,
            out string error)
        {
            var hoy = DateTime.Today;

            fechaInicio = hoy;
            fechaFinExclusiva = hoy.AddDays(1);
            periodoNormalizado = string.Empty;
            error = string.Empty;

            var clave = periodo
                .Trim()
                .ToLowerInvariant()
                .Replace(" ", "")
                .Replace("-", "");

            switch (clave)
            {
                case "hoy":
                    fechaInicio = hoy;
                    periodoNormalizado = "Hoy";
                    break;

                case "semana":
                    var diasDesdeLunes =
                        ((int)hoy.DayOfWeek + 6) % 7;

                    fechaInicio = hoy.AddDays(-diasDesdeLunes);
                    periodoNormalizado = "Esta semana";
                    break;

                case "mes":
                    fechaInicio = new DateTime(
                        hoy.Year, hoy.Month, 1);

                    periodoNormalizado = "Este mes";
                    break;

                case "ultimos3meses":
                case "últimos3meses":
                    fechaInicio = hoy.AddMonths(-3);
                    periodoNormalizado = "Últimos 3 meses";
                    break;

                case "ultimos6meses":
                case "últimos6meses":
                    fechaInicio = hoy.AddMonths(-6);
                    periodoNormalizado = "Últimos 6 meses";
                    break;

                case "ultimos12meses":
                case "últimos12meses":
                    fechaInicio = hoy.AddMonths(-12);
                    periodoNormalizado = "Últimos 12 meses";
                    break;

                case "personalizado":
                    if (!desde.HasValue || !hasta.HasValue)
                    {
                        error =
                            "Para el período personalizado debes indicar desde y hasta.";
                        return false;
                    }

                    if (desde.Value.Date > hasta.Value.Date)
                    {
                        error =
                            "La fecha inicial no puede ser posterior a la fecha final.";
                        return false;
                    }

                    fechaInicio = desde.Value.Date;
                    fechaFinExclusiva = hasta.Value.Date.AddDays(1);
                    periodoNormalizado = "Personalizado";
                    return true;

                default:
                    error =
                        "Período no válido. Utiliza hoy, semana, mes, " +
                        "ultimos3meses, ultimos6meses, ultimos12meses " +
                        "o personalizado.";
                    return false;
            }

            fechaInicio = fechaInicio.Date;
            fechaFinExclusiva = hoy.AddDays(1);

            return true;
        }
    }
}
