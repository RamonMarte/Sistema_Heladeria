using Heladeria.Contracts.Categorias;
using Heladeria.Contracts.Productos;
using Heladeria.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Heladeria.Desktop.ViewModels
{
    public class ProductosViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;

        private List<CategoriaResponse> _categoriasCache = [];
        private List<ProductoResponse> _productosCache = [];

        public ObservableCollection<CategoriaResponse> Categorias { get; } = [];
        public ObservableCollection<CategoriaResponse> CategoriasActivas { get; } = [];
        public ObservableCollection<ProductoResponse> Productos { get; } = [];
        public ObservableCollection<PresentacionResponse> Presentaciones { get; } = [];

        private CategoriaResponse? _categoriaSeleccionada;
        private string _nombreCategoria = "";
        private string _descripcionCategoria = "";
        private bool _categoriaActiva = true;

        private string _buscarProducto = "";
        private ProductoResponse? _productoSeleccionado;
        private CategoriaResponse? _categoriaProductoSeleccionada;
        private string _nombreProducto = "";
        private string _saborProducto = "";
        private string _descripcionProducto = "";
        private bool _productoActivo = true;

        private PresentacionResponse? _presentacionSeleccionada;
        private string _nombrePresentacion = "";
        private string _tamano = "";
        private string _codigoInterno = "";
        private string _codigoBarras = "";
        private string _precioVentaTexto = "";
        private string _costoEstimadoTexto = "";
        private bool _presentacionActiva = true;

        private string _mensaje = "";
        private bool _ocupado;

        public AsyncRelayCommand CargarCommand { get; }
        public AsyncRelayCommand GuardarCategoriaCommand { get; }
        public AsyncRelayCommand GuardarProductoCommand { get; }
        public AsyncRelayCommand GuardarPresentacionCommand { get; }

        public RelayCommand NuevaCategoriaCommand { get; }
        public RelayCommand NuevoProductoCommand { get; }
        public RelayCommand NuevaPresentacionCommand { get; }

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
                {
                    CargarCommand.NotificarCambio();
                    GuardarCategoriaCommand.NotificarCambio();
                    GuardarProductoCommand.NotificarCambio();
                    GuardarPresentacionCommand.NotificarCambio();
                }
            }
        }

        public string NombreCategoria
        {
            get => _nombreCategoria;
            set
            {
                if (SetProperty(ref _nombreCategoria, value))
                    GuardarCategoriaCommand.NotificarCambio();
            }
        }

        public string DescripcionCategoria
        {
            get => _descripcionCategoria;
            set => SetProperty(ref _descripcionCategoria, value);
        }

        public bool CategoriaActiva
        {
            get => _categoriaActiva;
            set => SetProperty(ref _categoriaActiva, value);
        }

        public CategoriaResponse? CategoriaSeleccionada
        {
            get => _categoriaSeleccionada;
            set
            {
                if (!SetProperty(ref _categoriaSeleccionada, value))
                    return;

                NombreCategoria = value?.Nombre ?? "";
                DescripcionCategoria = value?.Descripcion ?? "";
                CategoriaActiva = value?.Activa ?? true;

                GuardarCategoriaCommand.NotificarCambio();
            }
        }

        public string BuscarProducto
        {
            get => _buscarProducto;
            set
            {
                if (SetProperty(ref _buscarProducto, value))
                    AplicarFiltroProductos();
            }
        }

        public string NombreProducto
        {
            get => _nombreProducto;
            set
            {
                if (SetProperty(ref _nombreProducto, value))
                    GuardarProductoCommand.NotificarCambio();
            }
        }

        public string SaborProducto
        {
            get => _saborProducto;
            set => SetProperty(ref _saborProducto, value);
        }

        public string DescripcionProducto
        {
            get => _descripcionProducto;
            set => SetProperty(ref _descripcionProducto, value);
        }

        public bool ProductoActivo
        {
            get => _productoActivo;
            set => SetProperty(ref _productoActivo, value);
        }

        public CategoriaResponse? CategoriaProductoSeleccionada
        {
            get => _categoriaProductoSeleccionada;
            set
            {
                if (SetProperty(ref _categoriaProductoSeleccionada, value))
                    GuardarProductoCommand.NotificarCambio();
            }
        }

        public ProductoResponse? ProductoSeleccionado
        {
            get => _productoSeleccionado;
            set
            {
                if (!SetProperty(ref _productoSeleccionado, value))
                    return;

                NombreProducto = value?.Nombre ?? "";
                SaborProducto = value?.Sabor ?? "";
                DescripcionProducto = value?.Descripcion ?? "";
                ProductoActivo = value?.Activo ?? true;

                CategoriaProductoSeleccionada =
                    CategoriasActivas.FirstOrDefault(c =>
                        c.CategoriaId == value?.CategoriaId);

                Presentaciones.Clear();
                PresentacionSeleccionada = null;

                if (value is not null)
                {
                    foreach (var presentacion in value.Presentaciones
                                 .OrderBy(p => p.NombrePresentacion))
                    {
                        Presentaciones.Add(presentacion);
                    }
                }

                OnPropertyChanged(nameof(HayProductoSeleccionado));

                GuardarProductoCommand.NotificarCambio();
                GuardarPresentacionCommand.NotificarCambio();
            }
        }

        public bool HayProductoSeleccionado =>
            ProductoSeleccionado is not null;

        public string NombrePresentacion
        {
            get => _nombrePresentacion;
            set
            {
                if (SetProperty(ref _nombrePresentacion, value))
                    GuardarPresentacionCommand.NotificarCambio();
            }
        }

        public string Tamano
        {
            get => _tamano;
            set => SetProperty(ref _tamano, value);
        }

        public string CodigoInterno
        {
            get => _codigoInterno;
            set
            {
                if (SetProperty(ref _codigoInterno, value))
                    GuardarPresentacionCommand.NotificarCambio();
            }
        }

        public string CodigoBarras
        {
            get => _codigoBarras;
            set => SetProperty(ref _codigoBarras, value);
        }

        public string PrecioVentaTexto
        {
            get => _precioVentaTexto;
            set
            {
                if (SetProperty(ref _precioVentaTexto, value))
                    GuardarPresentacionCommand.NotificarCambio();
            }
        }

        public string CostoEstimadoTexto
        {
            get => _costoEstimadoTexto;
            set
            {
                if (SetProperty(ref _costoEstimadoTexto, value))
                    GuardarPresentacionCommand.NotificarCambio();
            }
        }

        public bool PresentacionActiva
        {
            get => _presentacionActiva;
            set => SetProperty(ref _presentacionActiva, value);
        }

        public PresentacionResponse? PresentacionSeleccionada
        {
            get => _presentacionSeleccionada;
            set
            {
                if (!SetProperty(ref _presentacionSeleccionada, value))
                    return;

                NombrePresentacion = value?.NombrePresentacion ?? "";
                Tamano = value?.Tamano ?? "";
                CodigoInterno = value?.CodigoInterno ?? "";
                CodigoBarras = value?.CodigoBarras ?? "";

                PrecioVentaTexto = value is null
                    ? ""
                    : value.PrecioVenta.ToString("0.00", CultureInfo.CurrentCulture);

                CostoEstimadoTexto = value is null
                    ? ""
                    : value.CostoEstimado.ToString("0.00", CultureInfo.CurrentCulture);

                PresentacionActiva = value?.Activa ?? true;

                GuardarPresentacionCommand.NotificarCambio();
            }
        }

        public ProductosViewModel(ApiClient apiClient)
        {
            _apiClient = apiClient;

            CargarCommand = new AsyncRelayCommand(CargarAsync);

            GuardarCategoriaCommand = new AsyncRelayCommand(
                GuardarCategoriaAsync,
                () => !Ocupado && NombreCategoria.Trim().Length >= 2);

            GuardarProductoCommand = new AsyncRelayCommand(
                GuardarProductoAsync,
                () => !Ocupado &&
                      NombreProducto.Trim().Length >= 2 &&
                      CategoriaProductoSeleccionada is { Activa: true });

            GuardarPresentacionCommand = new AsyncRelayCommand(
                GuardarPresentacionAsync,
                PuedeGuardarPresentacion);

            NuevaCategoriaCommand = new RelayCommand(
                _ => CategoriaSeleccionada = null,
                _ => !Ocupado);

            NuevoProductoCommand = new RelayCommand(
                _ => ProductoSeleccionado = null,
                _ => !Ocupado);

            NuevaPresentacionCommand = new RelayCommand(
                _ => PresentacionSeleccionada = null,
                _ => !Ocupado && ProductoSeleccionado is { Activo: true });
        }

        public async Task InicializarAsync()
        {
            await CargarAsync();
        }

        private async Task CargarAsync()
        {
            if (Ocupado)
                return;

            Ocupado = true;

            try
            {
                var categoriaId = CategoriaSeleccionada?.CategoriaId;
                var productoId = ProductoSeleccionado?.ProductoId;
                var presentacionId =
                    PresentacionSeleccionada?.ProductoPresentacionId;

                _categoriasCache =
                    await ObtenerJsonAsync<List<CategoriaResponse>>(
                        "api/categorias?incluirInactivas=true");

                _productosCache =
                    await ObtenerJsonAsync<List<ProductoResponse>>(
                        "api/productos?incluirInactivos=true");

                Categorias.Clear();
                CategoriasActivas.Clear();

                foreach (var categoria in _categoriasCache.OrderBy(c => c.Nombre))
                {
                    Categorias.Add(categoria);

                    if (categoria.Activa)
                        CategoriasActivas.Add(categoria);
                }

                CategoriaSeleccionada = categoriaId.HasValue
                    ? Categorias.FirstOrDefault(c =>
                        c.CategoriaId == categoriaId.Value)
                    : null;

                AplicarFiltroProductos();

                ProductoSeleccionado = productoId.HasValue
                    ? Productos.FirstOrDefault(p =>
                        p.ProductoId == productoId.Value)
                    : null;

                if (presentacionId.HasValue)
                {
                    PresentacionSeleccionada =
                        Presentaciones.FirstOrDefault(p =>
                            p.ProductoPresentacionId == presentacionId.Value);
                }

                Mensaje = "Catálogo actualizado correctamente.";
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

        private void AplicarFiltroProductos()
        {
            var texto = BuscarProducto.Trim();

            var resultados = string.IsNullOrWhiteSpace(texto)
                ? _productosCache
                : _productosCache.Where(p =>
                    p.Nombre.Contains(
                        texto, StringComparison.OrdinalIgnoreCase) ||
                    (p.Sabor?.Contains(
                        texto, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    p.CategoriaNombre.Contains(
                        texto, StringComparison.OrdinalIgnoreCase) ||
                    p.Presentaciones.Any(pr =>
                        pr.CodigoInterno.Contains(
                            texto, StringComparison.OrdinalIgnoreCase) ||
                        (pr.CodigoBarras?.Contains(
                            texto, StringComparison.OrdinalIgnoreCase) ?? false)))
                    .ToList();

            var idSeleccionado = ProductoSeleccionado?.ProductoId;

            Productos.Clear();

            foreach (var producto in resultados.OrderBy(p => p.Nombre))
                Productos.Add(producto);

            if (idSeleccionado.HasValue)
            {
                ProductoSeleccionado = Productos.FirstOrDefault(p =>
                    p.ProductoId == idSeleccionado.Value);
            }
            else
            {
                GuardarProductoCommand.NotificarCambio();
            }
        }

        private async Task GuardarCategoriaAsync()
        {
            if (Ocupado)
                return;

            Ocupado = true;

            try
            {
                var id = CategoriaSeleccionada?.CategoriaId;
                var nombre = NombreCategoria.Trim();

                HttpResponseMessage response;

                if (id.HasValue)
                {
                    response = await _apiClient.Http.PutAsJsonAsync(
                        $"api/categorias/{id.Value}",
                        new ActualizarCategoriaRequest
                        {
                            Nombre = nombre,
                            Descripcion = Limpiar(DescripcionCategoria),
                            Activa = CategoriaActiva
                        });
                }
                else
                {
                    response = await _apiClient.Http.PostAsJsonAsync(
                        "api/categorias",
                        new CrearCategoriaRequest
                        {
                            Nombre = nombre,
                            Descripcion = Limpiar(DescripcionCategoria)
                        });
                }

                using (response)
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        Mensaje = await LeerErrorAsync(response);
                        return;
                    }

                    var creado = id.HasValue
                        ? id.Value
                        : (await response.Content.ReadFromJsonAsync<CategoriaResponse>())
                            ?.CategoriaId ?? 0;

                    await CargarSinBloqueoAsync();

                    CategoriaSeleccionada = Categorias.FirstOrDefault(c =>
                        c.CategoriaId == creado);

                    Mensaje = id.HasValue
                        ? "Categoría actualizada correctamente."
                        : "Categoría creada correctamente.";
                }
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

        private async Task GuardarProductoAsync()
        {
            if (Ocupado || CategoriaProductoSeleccionada is null)
                return;

            Ocupado = true;

            try
            {
                var id = ProductoSeleccionado?.ProductoId;
                HttpResponseMessage response;

                if (id.HasValue)
                {
                    response = await _apiClient.Http.PutAsJsonAsync(
                        $"api/productos/{id.Value}",
                        new ActualizarProductoRequest
                        {
                            CategoriaId =
                                CategoriaProductoSeleccionada.CategoriaId,
                            Nombre = NombreProducto.Trim(),
                            Sabor = Limpiar(SaborProducto),
                            Descripcion = Limpiar(DescripcionProducto),
                            Activo = ProductoActivo
                        });
                }
                else
                {
                    response = await _apiClient.Http.PostAsJsonAsync(
                        "api/productos",
                        new CrearProductoRequest
                        {
                            CategoriaId =
                                CategoriaProductoSeleccionada.CategoriaId,
                            Nombre = NombreProducto.Trim(),
                            Sabor = Limpiar(SaborProducto),
                            Descripcion = Limpiar(DescripcionProducto)
                        });
                }

                using (response)
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        Mensaje = await LeerErrorAsync(response);
                        return;
                    }

                    var creado = id ?? await LeerIdAsync(
                        response,
                        "productoId",
                        "ProductoId");

                    await CargarSinBloqueoAsync();

                    ProductoSeleccionado = Productos.FirstOrDefault(p =>
                        p.ProductoId == creado);

                    Mensaje = id.HasValue
                        ? "Producto actualizado correctamente."
                        : "Producto creado correctamente.";
                }
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

        private async Task GuardarPresentacionAsync()
        {
            if (Ocupado || ProductoSeleccionado is null)
                return;

            if (!IntentarLeerImporte(PrecioVentaTexto, out var precio) ||
                !IntentarLeerImporte(CostoEstimadoTexto, out var costo) ||
                precio < 0 || costo < 0)
            {
                Mensaje =
                    "Introduce un precio de venta y costo válidos y no negativos.";
                return;
            }

            Ocupado = true;

            try
            {
                var productoId = ProductoSeleccionado.ProductoId;
                var presentacionId =
                    PresentacionSeleccionada?.ProductoPresentacionId;

                HttpResponseMessage response;

                if (presentacionId.HasValue)
                {
                    response = await _apiClient.Http.PutAsJsonAsync(
                        $"api/productos/presentaciones/{presentacionId.Value}",
                        new ActualizarPresentacionRequest
                        {
                            NombrePresentacion = NombrePresentacion.Trim(),
                            Tamano = Limpiar(Tamano),
                            CodigoInterno = CodigoInterno.Trim(),
                            CodigoBarras = Limpiar(CodigoBarras),
                            PrecioVenta = precio,
                            CostoEstimado = costo,
                            Activa = PresentacionActiva
                        });
                }
                else
                {
                    response = await _apiClient.Http.PostAsJsonAsync(
                        $"api/productos/{productoId}/presentaciones",
                        new CrearPresentacionRequest
                        {
                            NombrePresentacion = NombrePresentacion.Trim(),
                            Tamano = Limpiar(Tamano),
                            CodigoInterno = CodigoInterno.Trim(),
                            CodigoBarras = Limpiar(CodigoBarras),
                            PrecioVenta = precio,
                            CostoEstimado = costo
                        });
                }

                using (response)
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        Mensaje = await LeerErrorAsync(response);
                        return;
                    }

                    var creado = presentacionId ??
                        (await response.Content.ReadFromJsonAsync<PresentacionResponse>())
                            ?.ProductoPresentacionId ?? 0;

                    await CargarSinBloqueoAsync();

                    ProductoSeleccionado = Productos.FirstOrDefault(p =>
                        p.ProductoId == productoId);

                    PresentacionSeleccionada = Presentaciones.FirstOrDefault(p =>
                        p.ProductoPresentacionId == creado);

                    Mensaje = presentacionId.HasValue
                        ? "Presentación actualizada correctamente."
                        : "Presentación creada correctamente.";
                }
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

        private bool PuedeGuardarPresentacion()
        {
            return !Ocupado &&
                   ProductoSeleccionado is { Activo: true } &&
                   NombrePresentacion.Trim().Length >= 2 &&
                   !string.IsNullOrWhiteSpace(CodigoInterno) &&
                   IntentarLeerImporte(PrecioVentaTexto, out var precio) &&
                   precio >= 0 &&
                   IntentarLeerImporte(CostoEstimadoTexto, out var costo) &&
                   costo >= 0;
        }

        private async Task CargarSinBloqueoAsync()
        {
            var categoriaActual = CategoriaSeleccionada?.CategoriaId;
            var productoActual = ProductoSeleccionado?.ProductoId;
            var presentacionActual =
                PresentacionSeleccionada?.ProductoPresentacionId;

            _categoriasCache =
                await ObtenerJsonAsync<List<CategoriaResponse>>(
                    "api/categorias?incluirInactivas=true");

            _productosCache =
                await ObtenerJsonAsync<List<ProductoResponse>>(
                    "api/productos?incluirInactivos=true");

            Categorias.Clear();
            CategoriasActivas.Clear();

            foreach (var categoria in _categoriasCache.OrderBy(c => c.Nombre))
            {
                Categorias.Add(categoria);

                if (categoria.Activa)
                    CategoriasActivas.Add(categoria);
            }

            AplicarFiltroProductos();

            CategoriaSeleccionada = categoriaActual.HasValue
                ? Categorias.FirstOrDefault(c =>
                    c.CategoriaId == categoriaActual.Value)
                : null;

            ProductoSeleccionado = productoActual.HasValue
                ? Productos.FirstOrDefault(p =>
                    p.ProductoId == productoActual.Value)
                : null;

            PresentacionSeleccionada = presentacionActual.HasValue
                ? Presentaciones.FirstOrDefault(p =>
                    p.ProductoPresentacionId == presentacionActual.Value)
                : null;
        }

        private async Task<T> ObtenerJsonAsync<T>(string url)
        {
            using var response = await _apiClient.Http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException(
                    await LeerErrorAsync(response));

            return await response.Content.ReadFromJsonAsync<T>()
                ?? throw new InvalidOperationException(
                    "La API devolvió una respuesta vacía.");
        }

        private static async Task<int> LeerIdAsync(
            HttpResponseMessage response,
            string propiedadMinuscula,
            string propiedadPascal)
        {
            using var json = await response.Content.ReadFromJsonAsync<JsonDocument>();

            if (json is null)
                return 0;

            var raiz = json.RootElement;

            if (raiz.TryGetProperty(propiedadMinuscula, out var valor) ||
                raiz.TryGetProperty(propiedadPascal, out valor))
            {
                return valor.GetInt32();
            }

            return 0;
        }

        private static string? Limpiar(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor)
                ? null
                : valor.Trim();
        }

        private static bool IntentarLeerImporte(
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
                                ?? "Revisa los datos introducidos.";
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // La respuesta no contenía JSON.
            }

            return $"La API respondió con el código {(int)response.StatusCode}.";
        }
    }
}
