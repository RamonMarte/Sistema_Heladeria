using System;
using System.Collections.Generic;
using System.Text;

namespace Heladeria.Contracts.Reportes
{
    public sealed class ReporteVentasDashboardResponse
    {
        public string Periodo { get; set; } = string.Empty;

        public DateTimeOffset FechaDesde { get; set; }

        public DateTimeOffset FechaHasta { get; set; }

        public int CantidadTransacciones { get; set; }

        public decimal ImporteTotalTickets { get; set; }

        public decimal DescuentosTickets { get; set; }

        public decimal UnidadesVendidas { get; set; }

        public decimal ImporteProductos { get; set; }

        public decimal CostoEstimadoProductos { get; set; }

        public decimal GananciaBrutaEstimada { get; set; }

        public List<PagoMetodoReporteDashboardResponse> PagosPorMetodo
        {
            get;
            set;
        } = new();

        public List<ProductoMasVendidoDashboardResponse> ProductosMasVendidos
        {
            get;
            set;
        } = new();

        public List<VentaPorDiaDashboardResponse> VentasPorDia
        {
            get;
            set;
        } = new();
    }

    public sealed class PagoMetodoReporteDashboardResponse
    {
        public string MetodoPago { get; set; } = string.Empty;

        public int CantidadPagos { get; set; }

        public decimal Importe { get; set; }
    }

    public sealed class ProductoMasVendidoDashboardResponse
    {
        public int ProductoPresentacionId { get; set; }

        public string NombreProducto { get; set; } = string.Empty;

        public string NombrePresentacion { get; set; } = string.Empty;

        public decimal CantidadVendida { get; set; }

        public decimal Importe { get; set; }

        public decimal CostoEstimado { get; set; }

        public decimal GananciaBrutaEstimada { get; set; }
    }

    public sealed class VentaPorDiaDashboardResponse
    {
        public DateTime Fecha { get; set; }

        public int CantidadTransacciones { get; set; }

        public decimal ImporteTotal { get; set; }
    }

    public sealed class VentaEmpleadoDashboardResponse
    {
        public int UsuarioId { get; set; }

        public string NombreUsuario { get; set; } = string.Empty;

        public string NombreCompleto { get; set; } = string.Empty;

        public int CantidadTransacciones { get; set; }

        public decimal ImporteTotalTickets { get; set; }

        public decimal UnidadesVendidas { get; set; }

        public decimal ImporteProductos { get; set; }

        public decimal CostoEstimadoProductos { get; set; }

        public decimal GananciaBrutaEstimada { get; set; }
    }
}
