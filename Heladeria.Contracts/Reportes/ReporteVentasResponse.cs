using System;
using System.Collections.Generic;
using System.Text;

namespace Heladeria.Contracts.Reportes
{
    public class ReporteVentasResponse
    {
        public string Periodo { get; set; } = string.Empty;

        public DateTime FechaDesde { get; set; }

        public DateTime FechaHasta { get; set; }

        public long CantidadTransacciones { get; set; }

        public decimal ImporteTotalTickets { get; set; }

        public decimal DescuentosTickets { get; set; }

        public decimal UnidadesVendidas { get; set; }

        public decimal ImporteProductos { get; set; }

        public decimal CostoEstimadoProductos { get; set; }

        public decimal GananciaBrutaEstimada { get; set; }

        public List<PagoMetodoReporteResponse> PagosPorMetodo { get; set; }
            = [];

        public List<ProductoVendidoReporteResponse> ProductosMasVendidos
        { get; set; } = [];

        public List<VentaDiaReporteResponse> VentasPorDia { get; set; } = [];
    }

    public class PagoMetodoReporteResponse
    {
        public string MetodoPago { get; set; } = string.Empty;

        public long CantidadPagos { get; set; }

        public decimal Importe { get; set; }
    }

    public class ProductoVendidoReporteResponse
    {
        public int ProductoPresentacionId { get; set; }

        public string NombreProducto { get; set; } = string.Empty;

        public string NombrePresentacion { get; set; } = string.Empty;

        public decimal CantidadVendida { get; set; }

        public decimal Importe { get; set; }

        public decimal CostoEstimado { get; set; }

        public decimal GananciaBrutaEstimada { get; set; }
    }

    public class VentaDiaReporteResponse
    {
        public DateTime Fecha { get; set; }

        public long CantidadTransacciones { get; set; }

        public decimal ImporteTotal { get; set; }
    }
}
