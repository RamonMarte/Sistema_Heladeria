using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Heladeria.Contracts.Reportes;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Heladeria.Desktop.Services
{
    public static class ReportesPdfService
    {
        private static string Dinero(decimal importe) =>
       $"RD$ {importe.ToString("#,##0.00", CultureInfo.InvariantCulture)}";

        private static string Cantidad(decimal cantidad) =>
            cantidad.ToString("0.###", CultureInfo.InvariantCulture);

        private static IContainer CeldaEncabezado(IContainer container) =>
            container
                .Background(Colors.Grey.Lighten3)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Medium)
                .Padding(5);

        private static IContainer Celda(IContainer container) =>
            container
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(5);

        public static void Generar(
            ReporteVentasDashboardResponse reporte,
            IEnumerable<VentaEmpleadoDashboardResponse> ventasPorEmpleado,
            string rutaArchivo)
        {
            ArgumentNullException.ThrowIfNull(reporte);

            if (string.IsNullOrWhiteSpace(rutaArchivo))
                throw new ArgumentException(
                    "Debes indicar dónde guardar el PDF.",
                    nameof(rutaArchivo));

            var empleados = ventasPorEmpleado.ToList();

            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(documento =>
            {
                documento.Page(pagina =>
                {
                    pagina.Size(PageSizes.A4);
                    pagina.Margin(30);
                    pagina.DefaultTextStyle(
                        estilo => estilo.FontFamily("Arial").FontSize(9));

                    pagina.Header().Column(encabezado =>
                    {
                        encabezado.Item()
                            .Text("HELADERÍA")
                            .FontSize(20)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);

                        encabezado.Item()
                            .Text("Reporte de ventas y estadísticas")
                            .FontSize(13)
                            .SemiBold();

                        encabezado.Item().Text(
                            $"Período: {reporte.Periodo} | " +
                            $"Desde: {reporte.FechaDesde:dd/MM/yyyy} | " +
                            $"Hasta: {reporte.FechaHasta:dd/MM/yyyy}");

                        encabezado.Item().Text(
                            $"Generado el {DateTime.Now:dd/MM/yyyy HH:mm}");
                    });

                    pagina.Content().PaddingVertical(12).Column(contenido =>
                    {
                        contenido.Spacing(12);

                        // RESUMEN GENERAL

                        contenido.Item()
                            .Text("1. Resumen general")
                            .FontSize(12)
                            .Bold();

                        contenido.Item().Table(tabla =>
                        {
                            tabla.ColumnsDefinition(columnas =>
                            {
                                columnas.RelativeColumn(3);
                                columnas.RelativeColumn(2);
                            });

                            tabla.Header(cabecera =>
                            {
                                cabecera.Cell()
                                    .Element(CeldaEncabezado)
                                    .Text("Indicador").Bold();

                                cabecera.Cell()
                                    .Element(CeldaEncabezado)
                                    .Text("Resultado").Bold();
                            });

                            Fila(tabla, "Transacciones",
                                reporte.CantidadTransacciones.ToString());

                            Fila(tabla, "Ingresos totales",
                                Dinero(reporte.ImporteTotalTickets));

                            Fila(tabla, "Descuentos",
                                Dinero(reporte.DescuentosTickets));

                            Fila(tabla, "Unidades vendidas",
                                Cantidad(reporte.UnidadesVendidas));

                            Fila(tabla, "Importe de productos",
                                Dinero(reporte.ImporteProductos));

                            Fila(tabla, "Costo estimado",
                                Dinero(reporte.CostoEstimadoProductos));

                            Fila(tabla, "Ganancia bruta estimada",
                                Dinero(reporte.GananciaBrutaEstimada));
                        });

                        // MÉTODOS DE PAGO

                        contenido.Item()
                            .Text("2. Pagos por método")
                            .FontSize(12)
                            .Bold();

                        if (reporte.PagosPorMetodo.Count == 0)
                        {
                            contenido.Item().Text("No hay pagos registrados.");
                        }
                        else
                        {
                            contenido.Item().Table(tabla =>
                            {
                                tabla.ColumnsDefinition(columnas =>
                                {
                                    columnas.RelativeColumn(3);
                                    columnas.RelativeColumn(1);
                                    columnas.RelativeColumn(2);
                                });

                                tabla.Header(cabecera =>
                                {
                                    cabecera.Cell().Element(CeldaEncabezado)
                                        .Text("Método").Bold();

                                    cabecera.Cell().Element(CeldaEncabezado)
                                        .Text("Pagos").Bold();

                                    cabecera.Cell().Element(CeldaEncabezado)
                                        .Text("Importe").Bold();
                                });

                                foreach (var pago in reporte.PagosPorMetodo)
                                {
                                    tabla.Cell().Element(Celda)
                                        .Text(pago.MetodoPago);

                                    tabla.Cell().Element(Celda)
                                        .Text(pago.CantidadPagos.ToString());

                                    tabla.Cell().Element(Celda)
                                        .Text(Dinero(pago.Importe));
                                }
                            });
                        }

                        // PRODUCTOS MÁS VENDIDOS

                        contenido.Item()
                            .Text("3. Productos más vendidos")
                            .FontSize(12)
                            .Bold();

                        if (reporte.ProductosMasVendidos.Count == 0)
                        {
                            contenido.Item().Text("No hay productos vendidos.");
                        }
                        else
                        {
                            contenido.Item().Table(tabla =>
                            {
                                tabla.ColumnsDefinition(columnas =>
                                {
                                    columnas.RelativeColumn(3);
                                    columnas.RelativeColumn(2);
                                    columnas.RelativeColumn(1);
                                    columnas.RelativeColumn(2);
                                });

                                tabla.Header(cabecera =>
                                {
                                    cabecera.Cell().Element(CeldaEncabezado)
                                        .Text("Producto").Bold();

                                    cabecera.Cell().Element(CeldaEncabezado)
                                        .Text("Presentación").Bold();

                                    cabecera.Cell().Element(CeldaEncabezado)
                                        .Text("Cantidad").Bold();

                                    cabecera.Cell().Element(CeldaEncabezado)
                                        .Text("Importe").Bold();
                                });

                                foreach (var producto in reporte.ProductosMasVendidos)
                                {
                                    tabla.Cell().Element(Celda)
                                        .Text(producto.NombreProducto);

                                    tabla.Cell().Element(Celda)
                                        .Text(producto.NombrePresentacion);

                                    tabla.Cell().Element(Celda)
                                        .Text(Cantidad(producto.CantidadVendida));

                                    tabla.Cell().Element(Celda)
                                        .Text(Dinero(producto.Importe));
                                }
                            });
                        }

                        // VENTAS POR DÍA

                        contenido.Item()
                            .Text("4. Ventas por día")
                            .FontSize(12)
                            .Bold();

                        if (reporte.VentasPorDia.Count == 0)
                        {
                            contenido.Item().Text("No hay ventas en el período.");
                        }
                        else
                        {
                            contenido.Item().Table(tabla =>
                            {
                                tabla.ColumnsDefinition(columnas =>
                                {
                                    columnas.RelativeColumn(2);
                                    columnas.RelativeColumn(2);
                                    columnas.RelativeColumn(2);
                                });

                                tabla.Header(cabecera =>
                                {
                                    cabecera.Cell().Element(CeldaEncabezado)
                                        .Text("Fecha").Bold();

                                    cabecera.Cell().Element(CeldaEncabezado)
                                        .Text("Transacciones").Bold();

                                    cabecera.Cell().Element(CeldaEncabezado)
                                        .Text("Importe").Bold();
                                });

                                foreach (var venta in reporte.VentasPorDia)
                                {
                                    tabla.Cell().Element(Celda)
                                        .Text(venta.Fecha.ToString("dd/MM/yyyy"));

                                    tabla.Cell().Element(Celda)
                                        .Text(venta.CantidadTransacciones.ToString());

                                    tabla.Cell().Element(Celda)
                                        .Text(Dinero(venta.ImporteTotal));
                                }
                            });
                        }

                        // VENTAS POR EMPLEADO

                        contenido.Item()
                            .Text("5. Ventas por empleado")
                            .FontSize(12)
                            .Bold();

                        if (empleados.Count == 0)
                        {
                            contenido.Item().Text("No hay ventas por empleado.");
                        }
                        else
                        {
                            contenido.Item().Table(tabla =>
                            {
                                tabla.ColumnsDefinition(columnas =>
                                {
                                    columnas.RelativeColumn(3);
                                    columnas.RelativeColumn(1);
                                    columnas.RelativeColumn(1);
                                    columnas.RelativeColumn(2);
                                });

                                tabla.Header(cabecera =>
                                {
                                    cabecera.Cell().Element(CeldaEncabezado)
                                        .Text("Empleado").Bold();

                                    cabecera.Cell().Element(CeldaEncabezado)
                                        .Text("Ventas").Bold();

                                    cabecera.Cell().Element(CeldaEncabezado)
                                        .Text("Unidades").Bold();

                                    cabecera.Cell().Element(CeldaEncabezado)
                                        .Text("Ingresos").Bold();
                                });

                                foreach (var empleado in empleados)
                                {
                                    tabla.Cell().Element(Celda)
                                        .Text(empleado.NombreCompleto);

                                    tabla.Cell().Element(Celda)
                                        .Text(empleado.CantidadTransacciones.ToString());

                                    tabla.Cell().Element(Celda)
                                        .Text(Cantidad(empleado.UnidadesVendidas));

                                    tabla.Cell().Element(Celda)
                                        .Text(Dinero(empleado.ImporteTotalTickets));
                                }
                            });
                        }

                        contenido.Item().PaddingTop(8).Text(
                            "Nota: los costos y ganancias son estimaciones calculadas " +
                            "por la API. Deben revisarse si los costos de productos " +
                            "o recetas no están configurados correctamente.")
                            .FontSize(8)
                            .FontColor(Colors.Grey.Darken2);
                    });

                    pagina.Footer().AlignCenter().Text(texto =>
                    {
                        texto.Span("Página ");
                        texto.CurrentPageNumber();
                        texto.Span(" de ");
                        texto.TotalPages();
                    });
                });
            }).GeneratePdf(rutaArchivo);
        }

        private static void Fila(
            TableDescriptor tabla,
            string indicador,
            string resultado)
        {
            tabla.Cell().Element(Celda).Text(indicador);
            tabla.Cell().Element(Celda).Text(resultado);
        }
    }
}
