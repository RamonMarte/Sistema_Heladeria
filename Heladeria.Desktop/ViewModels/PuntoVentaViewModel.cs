using Heladeria.Contracts.Cajas;
using Heladeria.Contracts.Productos;
using Heladeria.Contracts.Ventas;
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
    public class PuntoVentaViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;
        private CancellationTokenSource? _busquedaCts;

        private string _buscar = string.Empty;
        private string _mensaje = string.Empty;
        private string _metodoPago = "Efectivo";
        private string _montoRecibidoTexto = string.Empty;
        private string _referencia = string.Empty;
        private CajaResponse? _cajaSeleccionada;
        private bool _estaOcupado;
        private bool _inicializado;

        public ObservableCollection<CatalogoVentaResponse> Productos { get; }
            = [];

        public ObservableCollection<CajaResponse> Cajas { get; }
            = [];

        public ObservableCollection<CarritoItemViewModel> Carrito { get; }
            = [];

        public ObservableCollection<string> MetodosPago { get; }
            = ["Efectivo", "Transferencia"];

        public AsyncRelayCommand CobrarCommand { get; }

        public RelayCommand AgregarCommand { get; }

        public RelayCommand QuitarCommand { get; }

        public RelayCommand LimpiarCarritoCommand { get; }

        public string Buscar
        {
            get => _buscar;
            set
            {
                if (SetProperty(ref _buscar, value) && _inicializado)
                    _ = BuscarConRetrasoAsync(value);
            }
        }

        public string Mensaje
        {
            get => _mensaje;
            private set => SetProperty(ref _mensaje, value);
        }

        public string MetodoPago
        {
            get => _metodoPago;
            set
            {
                if (SetProperty(ref _metodoPago, value))
                {
                    OnPropertyChanged(nameof(Cambio));
                    CobrarCommand.NotificarCambio();
                }
            }
        }

        public string MontoRecibidoTexto
        {
            get => _montoRecibidoTexto;
            set
            {
                if (SetProperty(ref _montoRecibidoTexto, value))
                {
                    OnPropertyChanged(nameof(Cambio));
                    CobrarCommand.NotificarCambio();
                }
            }
        }

        public string Referencia
        {
            get => _referencia;
            set => SetProperty(ref _referencia, value);
        }

        public CajaResponse? CajaSeleccionada
        {
            get => _cajaSeleccionada;
            set
            {
                if (SetProperty(ref _cajaSeleccionada, value))
                    CobrarCommand.NotificarCambio();
            }
        }

        public bool EstaOcupado
        {
            get => _estaOcupado;
            private set
            {
                if (SetProperty(ref _estaOcupado, value))
                {
                    CobrarCommand.NotificarCambio();
                    AgregarCommand.NotificarCambio();
                    LimpiarCarritoCommand.NotificarCambio();
                }
            }
        }

        public decimal Subtotal =>
            Carrito.Sum(item => item.Importe);

        public decimal Total => Subtotal;

        public decimal CantidadTotalCarrito =>
            Carrito.Sum(item => item.Cantidad);

        public string ResumenCantidadCarrito =>
            $"{CantidadTotalCarrito:0.###} unidades";

        public decimal Cambio
        {
            get
            {
                if (MetodoPago != "Efectivo" ||
                    !IntentarLeerMonto(out var recibido))
                    return 0;

                return Math.Max(0, recibido - Total);
            }
        }

        public PuntoVentaViewModel(ApiClient apiClient)
        {
            _apiClient = apiClient;

            AgregarCommand = new RelayCommand(
                p => AgregarProducto(p as CatalogoVentaResponse),
                p => !_estaOcupado && p is CatalogoVentaResponse);

            QuitarCommand = new RelayCommand(
                p => QuitarProducto(p as CarritoItemViewModel),
                p => !_estaOcupado && p is CarritoItemViewModel);

            LimpiarCarritoCommand = new RelayCommand(
                _ => Carrito.Clear(),
                _ => !_estaOcupado && Carrito.Count > 0);

            CobrarCommand = new AsyncRelayCommand(
                CobrarAsync,
                PuedeCobrar);

            Carrito.CollectionChanged += Carrito_CollectionChanged;
        }

        public async Task InicializarAsync()
        {
            try
            {
                var cajas = await _apiClient.Http
                    .GetFromJsonAsync<List<CajaResponse>>(
                        "api/cajas");

                Cajas.Clear();

                foreach (var caja in cajas ?? [])
                    Cajas.Add(caja);

                CajaSeleccionada = Cajas.FirstOrDefault();

                _inicializado = true;

                await CargarCatalogoAsync(
                    Buscar,
                    CancellationToken.None);

                if (Cajas.Count == 0)
                {
                    Mensaje =
                        "No hay cajas activas. El administrador debe crear una caja.";
                }
            }
            catch (HttpRequestException)
            {
                Mensaje =
                    "No se pudo conectar con la API. Comprueba que esté ejecutándose.";
            }
            catch (TaskCanceledException)
            {
                Mensaje = "La conexión tardó demasiado. Inténtalo nuevamente.";
            }
            catch (Exception)
            {
                Mensaje =
                    "No se pudo cargar el punto de venta. Revisa la API y sus respuestas.";
            }
        }

        private async Task BuscarConRetrasoAsync(string texto)
        {
            _busquedaCts?.Cancel();

            var cts = new CancellationTokenSource();
            _busquedaCts = cts;

            try
            {
                await Task.Delay(300, cts.Token);

                await CargarCatalogoAsync(texto, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Se ha escrito otra letra y la búsqueda anterior ya no se necesita.
            }
            catch (HttpRequestException)
            {
                if (!cts.IsCancellationRequested)
                    Mensaje = "No se pudo consultar el catálogo.";
            }
            catch (Exception)
            {
                if (!cts.IsCancellationRequested)
                    Mensaje = "Ocurrió un error al buscar productos.";
            }
        }

        private async Task CargarCatalogoAsync(
            string buscar,
            CancellationToken cancellationToken)
        {
            var url = string.IsNullOrWhiteSpace(buscar)
                ? "api/productos/catalogo"
                : $"api/productos/catalogo?buscar={Uri.EscapeDataString(buscar.Trim())}";

            using var response = await _apiClient.Http.GetAsync(
                url,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    "No se pudo consultar el catálogo.",
                    null,
                    response.StatusCode);
            }

            var productos =
                await response.Content.ReadFromJsonAsync<
                    List<CatalogoVentaResponse>>(
                        cancellationToken: cancellationToken) ?? [];

            if (cancellationToken.IsCancellationRequested)
                return;

            Productos.Clear();

            foreach (var producto in productos)
                Productos.Add(producto);

            if (Productos.Count == 0 && Cajas.Count > 0)
                Mensaje = "No se encontraron productos.";
            else if (Productos.Count > 0)
                Mensaje = $"{Productos.Count} presentaciones encontradas.";
        }

        private void AgregarProducto(CatalogoVentaResponse? producto)
        {
            if (producto is null || EstaOcupado)
                return;

            var existente = Carrito.FirstOrDefault(item =>
                item.Producto.ProductoPresentacionId ==
                producto.ProductoPresentacionId);

            if (existente is not null)
            {
                existente.Cantidad += 1;
            }
            else
            {
                existente = new CarritoItemViewModel(producto);
                Carrito.Add(existente);
            }

            Mensaje =
                $"{producto.NombreProducto} agregado. " +
                $"Llevas {existente.Cantidad:0.###} de esta presentación " +
                $"y {CantidadTotalCarrito:0.###} unidades en total.";
        }

        private void QuitarProducto(CarritoItemViewModel? item)
        {
            if (item is not null)
                Carrito.Remove(item);
        }

        private void Carrito_CollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems is not null)
            {
                foreach (CarritoItemViewModel item in e.OldItems)
                    item.PropertyChanged -= CarritoItem_PropertyChanged;
            }

            if (e.NewItems is not null)
            {
                foreach (CarritoItemViewModel item in e.NewItems)
                    item.PropertyChanged += CarritoItem_PropertyChanged;
            }

            ActualizarTotales();
        }

        private void CarritoItem_PropertyChanged(
            object? sender,
            System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CarritoItemViewModel.Cantidad) ||
                e.PropertyName == nameof(CarritoItemViewModel.Importe))
            {
                ActualizarTotales();
            }
        }

        private void ActualizarTotales()
        {
            OnPropertyChanged(nameof(Subtotal));
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(Cambio));

            OnPropertyChanged(nameof(CantidadTotalCarrito));
            OnPropertyChanged(nameof(ResumenCantidadCarrito));

            CobrarCommand.NotificarCambio();
            LimpiarCarritoCommand.NotificarCambio();

            
        }

        private bool PuedeCobrar()
        {
            if (EstaOcupado ||
                Carrito.Count == 0 ||
                CajaSeleccionada is null ||
                Total <= 0)
                return false;

            if (MetodoPago == "Transferencia")
                return true;

            return IntentarLeerMonto(out var recibido) &&
                   recibido >= Total;
        }

        private bool IntentarLeerMonto(out decimal monto)
        {
            var texto = MontoRecibidoTexto.Trim();

            if (texto.Contains('.') && !texto.Contains(','))
            {
                return decimal.TryParse(
                    texto,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out monto);
            }

            return decimal.TryParse(
                       texto,
                       NumberStyles.Number,
                       CultureInfo.CurrentCulture,
                       out monto)
                   || decimal.TryParse(
                       texto,
                       NumberStyles.Number,
                       CultureInfo.InvariantCulture,
                       out monto);
        }

        private async Task CobrarAsync()
        {
            if (!PuedeCobrar())
            {
                Mensaje =
                    "Selecciona una caja, agrega productos y revisa el importe recibido.";
                return;
            }

            if (MetodoPago == "Efectivo" &&
                !IntentarLeerMonto(out _))
            {
                Mensaje = "Introduce un importe de efectivo válido.";
                return;
            }

            EstaOcupado = true;

            try
            {
                var solicitud = new CrearVentaRequest
                {
                    CajaId = CajaSeleccionada!.CajaId,
                    MetodoPago = MetodoPago,

                    MontoRecibido = MetodoPago == "Efectivo" &&
                                    IntentarLeerMonto(out var recibido)
                        ? recibido
                        : null,

                    Referencia = string.IsNullOrWhiteSpace(Referencia)
                        ? null
                        : Referencia.Trim(),

                    Detalles = Carrito.Select(item =>
                        new CrearVentaDetalleRequest
                        {
                            ProductoPresentacionId =
                                item.Producto.ProductoPresentacionId,
                            Cantidad = item.Cantidad
                        }).ToList()
                };

                using var response = await _apiClient.Http.PostAsJsonAsync(
                    "api/ventas",
                    solicitud);

                var contenido =
                    await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Mensaje = LeerMensajeError(contenido);

                    if (response.StatusCode ==
                        System.Net.HttpStatusCode.Unauthorized)
                    {
                        Mensaje =
                            "La sesión expiró o no está autorizada. Vuelve a iniciar sesión.";
                    }

                    return;
                }

                string numeroVenta = string.Empty;
                decimal totalCobrado = Total;
                decimal cambioDevuelto = 0;

                using (var documento = JsonDocument.Parse(contenido))
                {
                    var raiz = documento.RootElement;

                    if (raiz.TryGetProperty("numeroVenta", out var numero))
                        numeroVenta = numero.GetString() ?? string.Empty;

                    if (raiz.TryGetProperty("total", out var total))
                        totalCobrado = total.GetDecimal();

                    if (raiz.TryGetProperty("cambio", out var cambio))
                        cambioDevuelto = cambio.GetDecimal();
                }

                Mensaje =
                    $"Venta {numeroVenta} registrada. " +
                    $"Total: RD$ {totalCobrado:N2}. " +
                    $"Cambio: RD$ {cambioDevuelto:N2}.";

                Carrito.Clear();
                MontoRecibidoTexto = string.Empty;
                Referencia = string.Empty;
            }
            catch (HttpRequestException)
            {
                Mensaje =
                    "No se pudo completar la venta. Comprueba la conexión con la API.";
            }
            catch (TaskCanceledException)
            {
                Mensaje =
                    "La operación tardó demasiado. Verifica el estado de la venta antes de volver a cobrar.";
            }
            catch (Exception)
            {
                Mensaje =
                    "Ocurrió un error al procesar la venta. Revisa la API antes de reintentar.";
            }
            finally
            {
                EstaOcupado = false;
            }
        }

        private static string LeerMensajeError(string contenido)
        {
            try
            {
                using var documento = JsonDocument.Parse(contenido);
                var raiz = documento.RootElement;

                if (raiz.TryGetProperty("mensaje", out var mensaje))
                    return mensaje.GetString() ?? "La venta no pudo registrarse.";

                if (raiz.TryGetProperty("title", out var titulo))
                    return titulo.GetString() ?? "La venta no pudo registrarse.";

                if (raiz.TryGetProperty("errors", out var errores))
                {
                    foreach (var propiedad in errores.EnumerateObject())
                    {
                        if (propiedad.Value.ValueKind == JsonValueKind.Array &&
                            propiedad.Value.GetArrayLength() > 0)
                        {
                            return propiedad.Value[0].GetString()
                                ?? "Revisa los datos de la venta.";
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // La respuesta no contenía JSON.
            }

            return "La API rechazó la operación. Comprueba el inventario y los datos enviados.";
        }
    }
}
