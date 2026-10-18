using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Heladeria.Contracts.Compras
{
    public class RecepcionarCompraRequest
    {
        [Required]
        [MinLength(1)]
        public List<RecepcionarCompraDetalleRequest> Detalles
        { get; set; } = [];
    }

    public class RecepcionarCompraDetalleRequest
    {
        [Range(1, long.MaxValue)]
        public long CompraDetalleId { get; set; }

        [StringLength(80)]
        public string? CodigoLote { get; set; }

        public DateOnly? FechaVencimiento { get; set; }
    }
}
