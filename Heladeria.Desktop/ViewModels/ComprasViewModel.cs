using Heladeria.Contracts.Compras;
using Heladeria.Contracts.Inventario;
using Heladeria.Contracts.Proveedores;
using Heladeria.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Heladeria.Desktop.ViewModels
{
    public class ComprasViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;

        public ObservableCollection<ProveedorResponse> Proveedores { get; } = [];
        public ObservableCollection<ProveedorResponse> ProveedoresActivos { get; } = [];
        public ObservableCollection<ArticuloInventarioResponse> Articulos { get; } = [];
        public ObservableCollection<CompraResponse> Compras { get; } = [];
        public ObservableCollection<CompraResponse> ComprasPendientes { get; } = [];

        public ObservableCollection<CompraLineaViewModel> LineasCompra { get; } = [];
        public ObservableCollection<RecepcionLineaViewModel> LineasRecepcion { get; } = [];

        public AsyncRelayCommand CargarCommand { get; }
        public AsyncRelayCommand GuardarProveedorCommand { get; }
        public AsyncRelayCommand CrearCompraCommand { get; }
        public AsyncRelayCommand RecibirCompraCommand { get; }

        public RelayCommand NuevoProveedorCommand { get; }
        public RelayCommand AgregarLineaCommand { get; }
        public RelayCommand QuitarLineaCommand { get; }
        public RelayCommand LimpiarCompraCommand { get; }

        private string _mensaje = string.Empty;
        private bool _ocupado;

        private ProveedorResponse? _proveedorSeleccionado;
        private string _nombreProveedor = string.Empty;
        private string _rnc = string.Empty;
        private string _telefono = string.Empty;
        private string _correo = string.Empty;
        private string _direccion = string.Empty;
        private bool _proveedorActivo = true;

        private ProveedorResponse? _proveedorParaCompra;
        private string _numeroCompra = string.Empty;
        private string _descuentoGeneralTexto = "0.00";
        private string _observacionesCompra = string.Empty;
        private CompraLineaViewModel? _lineaSeleccionada;

        private CompraResponse? _compraSeleccionadaRecepcion;

        public string Mensaje
        {
            get => _mensaje;
            private set => SetProperty(ref _mensaje, value);
        }

        public bool Ocupado
        {
            get => _ocupado;
            private set
            {
                if (SetProperty(ref _ocupado, value))
                    NotificarComandos();
            }
        }

        public ProveedorResponse? ProveedorSeleccionado
        {
            get => _proveedorSeleccionado;
            set
            {
                if (!SetProperty(ref _proveedorSeleccionado, value))
                    return;

                NombreProveedor = value?.Nombre ?? string.Empty;
                Rnc = value?.RNC ?? string.Empty;
                Telefono = value?.Telefono ?? string.Empty;
                Correo = value?.Correo ?? string.Empty;
                Direccion = value?.Direccion ?? string.Empty;
                ProveedorActivo = value?.Activo ?? true;

                GuardarProveedorCommand.NotificarCambio();
            }
        }

        public string NombreProveedor
        {
            get => _nombreProveedor;
            set
            {
                if (SetProperty(ref _nombreProveedor, value))
                    GuardarProveedorCommand.NotificarCambio();
            }
        }

        public string Rnc
        {
            get => _rnc;
            set => SetProperty(ref _rnc, value);
        }

        public string Telefono
        {
            get => _telefono;
            set => SetProperty(ref _telefono, value);
        }

        public string Correo
        {
            get => _correo;
            set => SetProperty(ref _correo, value);
        }

        public string Direccion
        {
            get => _direccion;
            set => SetProperty(ref _direccion, value);
        }

        public bool ProveedorActivo
        {
            get => _proveedorActivo;
            set => SetProperty(ref _proveedorActivo, value);
        }

        public ProveedorResponse? ProveedorParaCompra
        {
            get => _proveedorParaCompra;
            set
            {
                if (SetProperty(ref _proveedorParaCompra, value))
                    CrearCompraCommand.NotificarCambio();
            }
        }

        public string NumeroCompra
        {
            get => _numeroCompra;
            set
            {
                if (SetProperty(ref _numeroCompra, value))
                    CrearCompraCommand.NotificarCambio();
            }
        }

        public string DescuentoGeneralTexto
        {
            get => _descuentoGeneralTexto;
            set
            {
                if (SetProperty(ref _descuentoGeneralTexto, value))
                {
                    NotificarTotales();
                    CrearCompraCommand.NotificarCambio();
                }
            }
        }

        public string ObservacionesCompra
        {
            get => _observacionesCompra;
            set => SetProperty(ref _observacionesCompra, value);
        }

        public CompraLineaViewModel? LineaSeleccionada
        {
            get => _lineaSeleccionada;
            set => SetProperty(ref _lineaSeleccionada, value);
        }

        public CompraResponse? CompraSeleccionadaRecepcion
        {
            get => _compraSeleccionadaRecepcion;
            set
            {
                if (!SetProperty(ref _compraSeleccionadaRecepcion, value))
                    return;

                LineasRecepcion.Clear();

                if (value is not null && value.Estado == "Borrador")
                {
                    foreach (var detalle in value.Detalles)
                        LineasRecepcion.Add(new RecepcionLineaViewModel(detalle));
                }

                RecibirCompraCommand.NotificarCambio();
            }
        }

        public decimal SubtotalCompra =>
            LineasCompra.Sum(l => ObtenerBruto(l));

        public decimal DescuentoLineas =>
            LineasCompra.Sum(l => LeerDecimal(l.DescuentoTexto));

        public decimal DescuentoTotalCompra =>
            DescuentoLineas + LeerDecimal(DescuentoGeneralTexto);

        public decimal TotalCompra =>
            Math.Max(0, SubtotalCompra - DescuentoTotalCompra);

        public ComprasViewModel(ApiClient apiClient)
        {
            _apiClient = apiClient;

            LineasCompra.CollectionChanged += LineasCompra_CollectionChanged;

            CargarCommand = new AsyncRelayCommand(CargarAsync);

            GuardarProveedorCommand = new AsyncRelayCommand(
                GuardarProveedorAsync,
                () => !Ocupado && NombreProveedor.Trim().Length >= 2);

            CrearCompraCommand = new AsyncRelayCommand(
                CrearCompraAsync,
                PuedeCrearCompra);

            RecibirCompraCommand = new AsyncRelayCommand(
                RecibirCompraAsync,
                () => !Ocupado &&
                      CompraSeleccionadaRecepcion is
                      { Estado: "Borrador" } &&
                      LineasRecepcion.Count > 0);

            NuevoProveedorCommand = new RelayCommand(
                _ => LimpiarProveedor(),
                _ => !Ocupado);

            AgregarLineaCommand = new RelayCommand(
                _ => AgregarLinea(),
                _ => !Ocupado && Articulos.Count > 0);

            QuitarLineaCommand = new RelayCommand(
                p => QuitarLinea(p as CompraLineaViewModel),
                p => !Ocupado && p is CompraLineaViewModel);

            LimpiarCompraCommand = new RelayCommand(
                _ => LimpiarCompra(),
                _ => !Ocupado);
        }

        public async Task InicializarAsync() => await CargarAsync();

        private async Task CargarAsync()
        {
            if (Ocupado)
                return;

            Ocupado = true;

            try
            {
                await CargarDatosAsync();
                Mensaje = "Proveedores y compras actualizados.";
            }
            catch (Exception ex)
            {
                Mensaje = ex.Message;
            }
            finally
            {
                Ocupado = false;
            }
        }

        private async Task CargarDatosAsync()
        {
            var proveedorId = ProveedorSeleccionado?.ProveedorId;
            var compraRecepcionId = CompraSeleccionadaRecepcion?.CompraId;

            var proveedores = await ObtenerJsonAsync<List<ProveedorResponse>>(
                "api/proveedores?incluirInactivos=true");

            var articulos = await ObtenerJsonAsync<List<ArticuloInventarioResponse>>(
                "api/inventario/articulos");

            var compras = await ObtenerJsonAsync<List<CompraResponse>>(
                "api/compras");

            var pendientes = await ObtenerJsonAsync<List<CompraResponse>>(
                "api/compras?estado=Borrador");

            Proveedores.Clear();
            ProveedoresActivos.Clear();

            foreach (var proveedor in proveedores.OrderBy(p => p.Nombre))
            {
                Proveedores.Add(proveedor);

                if (proveedor.Activo)
                    ProveedoresActivos.Add(proveedor);
            }

            Articulos.Clear();

            foreach (var articulo in articulos.OrderBy(a => a.Nombre))
                Articulos.Add(articulo);

            Compras.Clear();

            foreach (var compra in compras.OrderByDescending(c => c.FechaCompra))
                Compras.Add(compra);

            ComprasPendientes.Clear();

            foreach (var compra in pendientes.OrderByDescending(c => c.FechaCompra))
                ComprasPendientes.Add(compra);

            ProveedorSeleccionado = proveedorId.HasValue
                ? Proveedores.FirstOrDefault(p => p.ProveedorId == proveedorId.Value)
                : null;

            CompraSeleccionadaRecepcion = compraRecepcionId.HasValue
                ? ComprasPendientes.FirstOrDefault(
                    c => c.CompraId == compraRecepcionId.Value)
                : null;

            if (LineasCompra.Count == 0 && Articulos.Count > 0)
                AgregarLinea();

            AgregarLineaCommand.NotificarCambio();
        }

        private async Task GuardarProveedorAsync()
        {
            if (Ocupado || NombreProveedor.Trim().Length < 2)
            {
                Mensaje = "Introduce el nombre del proveedor.";
                return;
            }

            Ocupado = true;

            try
            {
                var proveedorId = ProveedorSeleccionado?.ProveedorId;

                var request = new ProveedorRequest
                {
                    Nombre = NombreProveedor.Trim(),
                    RNC = Limpiar(Rnc),
                    Telefono = Limpiar(Telefono),
                    Correo = Limpiar(Correo),
                    Direccion = Limpiar(Direccion),
                    Activo = ProveedorActivo
                };

                using var response = proveedorId.HasValue
                    ? await _apiClient.Http.PutAsJsonAsync(
                        $"api/proveedores/{proveedorId.Value}", request)
                    : await _apiClient.Http.PostAsJsonAsync(
                        "api/proveedores", request);

                if (!response.IsSuccessStatusCode)
                {
                    Mensaje = await LeerErrorAsync(response);
                    return;
                }

                var resultado =
                    await response.Content.ReadFromJsonAsync<ProveedorResponse>();

                await CargarDatosAsync();

                if (resultado is not null)
                {
                    ProveedorSeleccionado = Proveedores.FirstOrDefault(
                        p => p.ProveedorId == resultado.ProveedorId);
                }

                Mensaje = proveedorId.HasValue
                    ? "Proveedor actualizado correctamente."
                    : "Proveedor registrado correctamente.";
            }
            catch (Exception ex)
            {
                Mensaje = ex.Message;
            }
            finally
            {
                Ocupado = false;
            }
        }

        private void LimpiarProveedor()
        {
            ProveedorSeleccionado = null;
            NombreProveedor = string.Empty;
            Rnc = string.Empty;
            Telefono = string.Empty;
            Correo = string.Empty;
            Direccion = string.Empty;
            ProveedorActivo = true;
        }

        private void AgregarLinea()
        {
            var linea = new CompraLineaViewModel();
            linea.Modificado += LineaCompra_Modificada;

            LineasCompra.Add(linea);
            NotificarTotales();
        }

        private void QuitarLinea(CompraLineaViewModel? linea)
        {
            if (linea is null)
                return;

            linea.Modificado -= LineaCompra_Modificada;
            LineasCompra.Remove(linea);

            NotificarTotales();
        }

        private void LineasCompra_CollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is not null)
            {
                foreach (CompraLineaViewModel linea in e.OldItems)
                    linea.Modificado -= LineaCompra_Modificada;
            }

            if (e.NewItems is not null)
            {
                foreach (CompraLineaViewModel linea in e.NewItems)
                    linea.Modificado += LineaCompra_Modificada;
            }

            NotificarTotales();
        }

        private void LineaCompra_Modificada()
        {
            NotificarTotales();
        }

        private void NotificarTotales()
        {
            OnPropertyChanged(nameof(SubtotalCompra));
            OnPropertyChanged(nameof(DescuentoLineas));
            OnPropertyChanged(nameof(DescuentoTotalCompra));
            OnPropertyChanged(nameof(TotalCompra));

            CrearCompraCommand.NotificarCambio();
        }

        private bool PuedeCrearCompra()
        {
            return !Ocupado &&
                   !string.IsNullOrWhiteSpace(NumeroCompra) &&
                   ProveedorParaCompra is { Activo: true } &&
                   LineasCompra.Count is > 0 and <= 100 &&
                   IntentarValidarLineas(out _) &&
                   IntentarLeerDecimal(DescuentoGeneralTexto, out var d) &&
                   d >= 0 &&
                   d == decimal.Round(d, 2);
        }

        private bool IntentarValidarLineas(out string error)
        {
            error = string.Empty;

            if (LineasCompra.Count == 0)
            {
                error = "Agrega al menos un artículo a la compra.";
                return false;
            }

            if (LineasCompra.Any(l => l.ArticuloSeleccionado is null))
            {
                error = "Selecciona el artículo de cada línea.";
                return false;
            }

            if (LineasCompra
                .GroupBy(l => l.ArticuloSeleccionado!.ArticuloInventarioId)
                .Any(g => g.Count() > 1))
            {
                error = "No repitas un artículo. Modifica su cantidad en la línea existente.";
                return false;
            }

            decimal subtotal = 0;
            decimal descuentos = 0;

            foreach (var linea in LineasCompra)
            {
                if (!IntentarLeerDecimal(linea.CantidadTexto, out var cantidad) ||
                    cantidad <= 0 ||
                    cantidad != decimal.Round(cantidad, 3))
                {
                    error = "Las cantidades deben ser positivas y admitir como máximo tres decimales.";
                    return false;
                }

                if (!IntentarLeerDecimal(linea.CostoUnitarioTexto, out var costo) ||
                    costo < 0 ||
                    costo != decimal.Round(costo, 4))
                {
                    error = "Los costos deben ser no negativos y admitir como máximo cuatro decimales.";
                    return false;
                }

                if (!IntentarLeerDecimal(linea.DescuentoTexto, out var descuento) ||
                    descuento < 0 ||
                    descuento != decimal.Round(descuento, 2))
                {
                    error = "Los descuentos deben ser no negativos y admitir como máximo dos decimales.";
                    return false;
                }

                var bruto = Math.Round(
                    cantidad * costo,
                    2,
                    MidpointRounding.AwayFromZero);

                if (descuento > bruto)
                {
                    error = "El descuento de una línea no puede superar su importe.";
                    return false;
                }

                subtotal += bruto;
                descuentos += descuento;
            }

            var descuentoGeneral = LeerDecimal(DescuentoGeneralTexto);

            if (descuentoGeneral > subtotal - descuentos)
            {
                error = "El descuento general supera el importe disponible.";
                return false;
            }

            return true;
        }

        private async Task CrearCompraAsync()
        {
            if (!IntentarValidarLineas(out var error))
            {
                Mensaje = error;
                return;
            }

            if (ProveedorParaCompra is null ||
                string.IsNullOrWhiteSpace(NumeroCompra))
            {
                Mensaje = "Indica el número de compra y selecciona un proveedor.";
                return;
            }

            Ocupado = true;

            try
            {
                var request = new CrearCompraRequest
                {
                    NumeroCompra = NumeroCompra.Trim(),
                    ProveedorId = ProveedorParaCompra.ProveedorId,
                    Descuento = LeerDecimal(DescuentoGeneralTexto),
                    Observaciones = Limpiar(ObservacionesCompra),

                    Detalles = LineasCompra.Select(l =>
                        new CrearCompraDetalleRequest
                        {
                            ArticuloInventarioId =
                                l.ArticuloSeleccionado!.ArticuloInventarioId,

                            Cantidad = LeerDecimal(l.CantidadTexto),

                            CostoUnitario =
                                LeerDecimal(l.CostoUnitarioTexto),

                            Descuento = LeerDecimal(l.DescuentoTexto)
                        }).ToList()
                };

                using var response = await _apiClient.Http.PostAsJsonAsync(
                    "api/compras", request);

                if (!response.IsSuccessStatusCode)
                {
                    Mensaje = await LeerErrorAsync(response);
                    return;
                }

                var creada =
                    await response.Content.ReadFromJsonAsync<CompraResponse>();

                var numero = NumeroCompra;

                LimpiarCompra();
                await CargarDatosAsync();

                Mensaje = creada is null
                    ? $"Compra {numero} registrada como borrador."
                    : $"Compra {creada.NumeroCompra} registrada como borrador. Total: RD$ {creada.Total:N2}.";
            }
            catch (HttpRequestException)
            {
                Mensaje = "No se pudo conectar con la API.";
            }
            catch (TaskCanceledException)
            {
                Mensaje = "La operación tardó demasiado. Comprueba el historial antes de repetir la compra.";
            }
            catch (Exception ex)
            {
                Mensaje = ex.Message;
            }
            finally
            {
                Ocupado = false;
            }
        }

        private void LimpiarCompra()
        {
            NumeroCompra = string.Empty;
            ProveedorParaCompra = null;
            DescuentoGeneralTexto = "0.00";
            ObservacionesCompra = string.Empty;

            foreach (var linea in LineasCompra)
                linea.Modificado -= LineaCompra_Modificada;

            LineasCompra.Clear();

            if (Articulos.Count > 0)
                AgregarLinea();

            NotificarTotales();
        }

        private async Task RecibirCompraAsync()
        {
            var compra = CompraSeleccionadaRecepcion;

            if (compra is null || compra.Estado != "Borrador")
            {
                Mensaje = "Selecciona una compra pendiente de recepción.";
                return;
            }

            if (LineasRecepcion.Count != compra.Detalles.Count)
            {
                Mensaje = "No se encontraron todas las líneas de la compra.";
                return;
            }

            Ocupado = true;

            try
            {
                var request = new RecepcionarCompraRequest
                {
                    Detalles = LineasRecepcion.Select(l =>
                        new RecepcionarCompraDetalleRequest
                        {
                            CompraDetalleId = l.CompraDetalleId,

                            CodigoLote = Limpiar(l.CodigoLote),

                            FechaVencimiento = l.FechaVencimiento.HasValue
                                ? DateOnly.FromDateTime(l.FechaVencimiento.Value)
                                : null
                        }).ToList()
                };

                using var response = await _apiClient.Http.PostAsJsonAsync(
                    $"api/compras/{compra.CompraId}/recibir", request);

                if (!response.IsSuccessStatusCode)
                {
                    Mensaje = await LeerErrorAsync(response);
                    return;
                }

                var numero = compra.NumeroCompra;

                await CargarDatosAsync();

                Mensaje =
                    $"La compra {numero} fue recibida correctamente. " +
                    "Se registraron los lotes y movimientos de inventario.";
            }
            catch (HttpRequestException)
            {
                Mensaje = "No se pudo conectar con la API.";
            }
            catch (TaskCanceledException)
            {
                Mensaje = "La operación tardó demasiado. Comprueba el estado de la compra antes de repetir la recepción.";
            }
            catch (Exception ex)
            {
                Mensaje = ex.Message;
            }
            finally
            {
                Ocupado = false;
            }
        }

        private async Task<T> ObtenerJsonAsync<T>(string url)
        {
            using var response = await _apiClient.Http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(await LeerErrorAsync(response));

            return await response.Content.ReadFromJsonAsync<T>()
                ?? throw new InvalidOperationException("La API devolvió una respuesta vacía.");
        }

        private static decimal ObtenerBruto(CompraLineaViewModel linea)
        {
            if (!IntentarLeerDecimal(linea.CantidadTexto, out var cantidad) ||
                !IntentarLeerDecimal(linea.CostoUnitarioTexto, out var costo))
                return 0;

            return Math.Round(
                cantidad * costo,
                2,
                MidpointRounding.AwayFromZero);
        }

        private static decimal LeerDecimal(string texto)
        {
            return IntentarLeerDecimal(texto, out var valor) ? valor : 0;
        }

        private static bool IntentarLeerDecimal(
            string texto,
            out decimal valor)
        {
            return decimal.TryParse(
                       texto,
                       NumberStyles.Number,
                       CultureInfo.CurrentCulture,
                       out valor)
                   || decimal.TryParse(
                       texto,
                       NumberStyles.Number,
                       CultureInfo.InvariantCulture,
                       out valor);
        }

        private static string? Limpiar(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor)
                ? null
                : valor.Trim();
        }

        private void NotificarComandos()
        {
            CargarCommand?.NotificarCambio();
            GuardarProveedorCommand?.NotificarCambio();
            CrearCompraCommand?.NotificarCambio();
            RecibirCompraCommand?.NotificarCambio();
            NuevoProveedorCommand?.NotificarCambio();
            AgregarLineaCommand?.NotificarCambio();
            QuitarLineaCommand?.NotificarCambio();
            LimpiarCompraCommand?.NotificarCambio();
        }

        private static async Task<string> LeerErrorAsync(
            HttpResponseMessage response)
        {
            var contenido = await response.Content.ReadAsStringAsync();

            try
            {
                using var documento = JsonDocument.Parse(contenido);
                var raiz = documento.RootElement;

                if (raiz.TryGetProperty("mensaje", out var mensaje))
                    return mensaje.GetString() ?? "La operación no pudo completarse.";

                if (raiz.TryGetProperty("title", out var titulo))
                    return titulo.GetString() ?? "La operación no pudo completarse.";

                if (raiz.TryGetProperty("errors", out var errores))
                {
                    foreach (var propiedad in errores.EnumerateObject())
                    {
                        if (propiedad.Value.ValueKind == JsonValueKind.Array &&
                            propiedad.Value.GetArrayLength() > 0)
                        {
                            return propiedad.Value[0].GetString()
                                ?? "Revisa los datos enviados.";
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // La respuesta no era JSON.
            }

            return $"La API respondió con el código {(int)response.StatusCode}.";
        }
    }
}
