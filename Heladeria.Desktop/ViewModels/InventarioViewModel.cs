using Heladeria.Contracts.Inventario;
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

    public class PresentacionInventarioOpcion
    {
        public int ProductoPresentacionId { get; set; }

        public string NombreCompleto { get; set; } = string.Empty;
    }

    public class InventarioViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;

        private List<ArticuloInventarioResponse> _todosArticulos = [];
        private List<ExistenciaInventarioResponse> _todasExistencias = [];

        public ObservableCollection<UnidadMedidaResponse> Unidades { get; } = [];
        public ObservableCollection<ArticuloInventarioResponse> Articulos { get; } = [];
        public ObservableCollection<ArticuloInventarioResponse> ArticulosActivos { get; } = [];
        public ObservableCollection<ExistenciaInventarioResponse> Existencias { get; } = [];
        public ObservableCollection<PresentacionInventarioOpcion> Presentaciones { get; } = [];

        public ObservableCollection<string> TiposArticulo { get; } =
            ["Ingrediente", "Material", "ProductoTerminado"];

        public ObservableCollection<string> MotivosMerma { get; } =
            ["Vencido", "Dañado", "Derrame", "Contaminado",
         "ConsumoInterno", "Otro"];

        private bool _ocupado;
        private string _mensaje = string.Empty;
        private string _buscarExistencias = string.Empty;
        private bool _soloBajoMinimo;

        private string _nombreArticulo = string.Empty;
        private string _tipoArticulo = "Ingrediente";
        private UnidadMedidaResponse? _unidadSeleccionada;
        private PresentacionInventarioOpcion? _presentacionNueva;
        private string _stockMinimoTexto = "0";

        private ArticuloInventarioResponse? _articuloMerma;
        private string _cantidadMermaTexto = string.Empty;
        private string _motivoMerma = "Dañado";
        private string _observacionesMerma = string.Empty;

        private ArticuloInventarioResponse? _articuloAjuste;
        private string _cantidadContadaTexto = string.Empty;
        private string _motivoAjuste = string.Empty;
        private string _costoEntradaTexto = string.Empty;
        private string _codigoLoteAjuste = string.Empty;
        private DateTime? _fechaVencimientoAjuste;
        private string _observacionesAjuste = string.Empty;

        public AsyncRelayCommand CargarCommand { get; }
        public AsyncRelayCommand RegistrarArticuloCommand { get; }
        public AsyncRelayCommand RegistrarMermaCommand { get; }
        public AsyncRelayCommand RegistrarAjusteCommand { get; }

        public RelayCommand LimpiarArticuloCommand { get; }
        public RelayCommand LimpiarMermaCommand { get; }
        public RelayCommand LimpiarAjusteCommand { get; }

        public bool Ocupado
        {
            get => _ocupado;
            private set
            {
                if (SetProperty(ref _ocupado, value))
                {
                    CargarCommand.NotificarCambio();
                    RegistrarArticuloCommand.NotificarCambio();
                    RegistrarMermaCommand.NotificarCambio();
                    RegistrarAjusteCommand.NotificarCambio();
                }
            }
        }

        public string Mensaje
        {
            get => _mensaje;
            private set => SetProperty(ref _mensaje, value);
        }

        public string BuscarExistencias
        {
            get => _buscarExistencias;
            set
            {
                if (SetProperty(ref _buscarExistencias, value))
                    AplicarFiltroExistencias();
            }
        }

        public bool SoloBajoMinimo
        {
            get => _soloBajoMinimo;
            set
            {
                if (SetProperty(ref _soloBajoMinimo, value))
                    AplicarFiltroExistencias();
            }
        }

        public string NombreArticulo
        {
            get => _nombreArticulo;
            set
            {
                if (SetProperty(ref _nombreArticulo, value))
                    RegistrarArticuloCommand.NotificarCambio();
            }
        }

        public string TipoArticulo
        {
            get => _tipoArticulo;
            set
            {
                if (SetProperty(ref _tipoArticulo, value))
                {
                    OnPropertyChanged(nameof(EsProductoTerminado));
                    RegistrarArticuloCommand.NotificarCambio();
                }
            }
        }

        public bool EsProductoTerminado =>
            TipoArticulo == "ProductoTerminado";

        public UnidadMedidaResponse? UnidadSeleccionada
        {
            get => _unidadSeleccionada;
            set
            {
                if (SetProperty(ref _unidadSeleccionada, value))
                    RegistrarArticuloCommand.NotificarCambio();
            }
        }

        public PresentacionInventarioOpcion? PresentacionNueva
        {
            get => _presentacionNueva;
            set
            {
                if (SetProperty(ref _presentacionNueva, value))
                    RegistrarArticuloCommand.NotificarCambio();
            }
        }

        public string StockMinimoTexto
        {
            get => _stockMinimoTexto;
            set
            {
                if (SetProperty(ref _stockMinimoTexto, value))
                    RegistrarArticuloCommand.NotificarCambio();
            }
        }

        public ArticuloInventarioResponse? ArticuloMerma
        {
            get => _articuloMerma;
            set
            {
                if (SetProperty(ref _articuloMerma, value))
                    RegistrarMermaCommand.NotificarCambio();
            }
        }

        public string CantidadMermaTexto
        {
            get => _cantidadMermaTexto;
            set
            {
                if (SetProperty(ref _cantidadMermaTexto, value))
                    RegistrarMermaCommand.NotificarCambio();
            }
        }

        public string MotivoMerma
        {
            get => _motivoMerma;
            set
            {
                if (SetProperty(ref _motivoMerma, value))
                    RegistrarMermaCommand.NotificarCambio();
            }
        }

        public string ObservacionesMerma
        {
            get => _observacionesMerma;
            set => SetProperty(ref _observacionesMerma, value);
        }

        public ArticuloInventarioResponse? ArticuloAjuste
        {
            get => _articuloAjuste;
            set
            {
                if (SetProperty(ref _articuloAjuste, value))
                    RegistrarAjusteCommand.NotificarCambio();
            }
        }

        public string CantidadContadaTexto
        {
            get => _cantidadContadaTexto;
            set
            {
                if (SetProperty(ref _cantidadContadaTexto, value))
                    RegistrarAjusteCommand.NotificarCambio();
            }
        }

        public string MotivoAjuste
        {
            get => _motivoAjuste;
            set
            {
                if (SetProperty(ref _motivoAjuste, value))
                    RegistrarAjusteCommand.NotificarCambio();
            }
        }

        public string CostoEntradaTexto
        {
            get => _costoEntradaTexto;
            set
            {
                if (SetProperty(ref _costoEntradaTexto, value))
                    RegistrarAjusteCommand.NotificarCambio();
            }
        }

        public string CodigoLoteAjuste
        {
            get => _codigoLoteAjuste;
            set => SetProperty(ref _codigoLoteAjuste, value);
        }

        public DateTime? FechaVencimientoAjuste
        {
            get => _fechaVencimientoAjuste;
            set => SetProperty(ref _fechaVencimientoAjuste, value);
        }

        public string ObservacionesAjuste
        {
            get => _observacionesAjuste;
            set => SetProperty(ref _observacionesAjuste, value);
        }

        public InventarioViewModel(ApiClient apiClient)
        {
            _apiClient = apiClient;

            CargarCommand = new AsyncRelayCommand(CargarAsync);

            RegistrarArticuloCommand = new AsyncRelayCommand(
                RegistrarArticuloAsync,
                () => !Ocupado &&
                      UnidadSeleccionada is not null &&
                      EsDecimalNoNegativo(StockMinimoTexto) &&
                      (EsProductoTerminado
                          ? PresentacionNueva is not null
                          : NombreArticulo.Trim().Length >= 2));

            RegistrarMermaCommand = new AsyncRelayCommand(
                RegistrarMermaAsync,
                () => !Ocupado &&
                      ArticuloMerma is not null &&
                      IntentarDecimal(CantidadMermaTexto, out var cantidad) &&
                      cantidad > 0 &&
                      !string.IsNullOrWhiteSpace(MotivoMerma));

            RegistrarAjusteCommand = new AsyncRelayCommand(
                RegistrarAjusteAsync,
                () => !Ocupado &&
                      ArticuloAjuste is not null &&
                      IntentarDecimal(CantidadContadaTexto, out var cantidad) &&
                      cantidad >= 0 &&
                      !string.IsNullOrWhiteSpace(MotivoAjuste));

            LimpiarArticuloCommand = new RelayCommand(
                _ => LimpiarArticulo(),
                _ => !Ocupado);

            LimpiarMermaCommand = new RelayCommand(
                _ => LimpiarMerma(),
                _ => !Ocupado);

            LimpiarAjusteCommand = new RelayCommand(
                _ => LimpiarAjuste(),
                _ => !Ocupado);
        }

        public Task InicializarAsync() => CargarAsync();

        private async Task CargarAsync()
        {
            if (Ocupado)
                return;

            Ocupado = true;

            try
            {
                await CargarDatosAsync();
                Mensaje = "Inventario actualizado correctamente.";
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
            var unidades = await ObtenerJsonAsync<List<UnidadMedidaResponse>>(
                "api/inventario/unidades");

            _todosArticulos =
                await ObtenerJsonAsync<List<ArticuloInventarioResponse>>(
                    "api/inventario/articulos?incluirInactivos=true");

            _todasExistencias =
                await ObtenerJsonAsync<List<ExistenciaInventarioResponse>>(
                    "api/inventario/existencias");

            var productos = await ObtenerJsonAsync<List<ProductoResponse>>(
                "api/productos?incluirInactivos=false");

            Unidades.Clear();

            foreach (var unidad in unidades)
                Unidades.Add(unidad);

            ArticulosActivos.Clear();

            foreach (var articulo in _todosArticulos
                         .Where(a => a.Activo)
                         .OrderBy(a => a.Nombre))
            {
                ArticulosActivos.Add(articulo);
            }

            Articulos.Clear();

            foreach (var articulo in _todosArticulos.OrderBy(a => a.Nombre))
                Articulos.Add(articulo);

            Presentaciones.Clear();

            foreach (var producto in productos)
            {
                foreach (var presentacion in producto.Presentaciones
                             .Where(p => p.Activa)
                             .OrderBy(p => p.NombrePresentacion))
                {
                    Presentaciones.Add(new PresentacionInventarioOpcion
                    {
                        ProductoPresentacionId =
                            presentacion.ProductoPresentacionId,

                        NombreCompleto =
                            $"{producto.Nombre} - {presentacion.NombrePresentacion}"
                    });
                }
            }

            AplicarFiltroExistencias();

            OnPropertyChanged(nameof(Articulos));
            RegistrarArticuloCommand.NotificarCambio();
        }

        private void AplicarFiltroExistencias()
        {
            var texto = BuscarExistencias.Trim();

            var resultado = _todasExistencias.Where(e =>
                (!SoloBajoMinimo || e.RequiereReposicion) &&
                (string.IsNullOrWhiteSpace(texto) ||
                 e.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                 e.TipoArticulo.Contains(texto, StringComparison.OrdinalIgnoreCase)));

            Existencias.Clear();

            foreach (var existencia in resultado.OrderBy(e => e.Nombre))
                Existencias.Add(existencia);
        }

        private async Task RegistrarArticuloAsync()
        {
            if (UnidadSeleccionada is null ||
                !IntentarDecimal(StockMinimoTexto, out var stockMinimo) ||
                stockMinimo < 0 ||
                stockMinimo != decimal.Round(stockMinimo, 3))
            {
                Mensaje = "Selecciona una unidad e introduce un stock mínimo válido.";
                return;
            }

            if (EsProductoTerminado && PresentacionNueva is null)
            {
                Mensaje = "Selecciona la presentación que deseas controlar.";
                return;
            }

            if (!EsProductoTerminado &&
                string.IsNullOrWhiteSpace(NombreArticulo))
            {
                Mensaje = "Escribe el nombre del ingrediente o material.";
                return;
            }

            Ocupado = true;

            try
            {
                var request = new CrearArticuloInventarioRequest
                {
                    Nombre = EsProductoTerminado
                        ? null
                        : NombreArticulo.Trim(),

                    TipoArticulo = TipoArticulo,

                    UnidadMedidaId = UnidadSeleccionada.UnidadMedidaId,

                    ProductoPresentacionId = EsProductoTerminado
                        ? PresentacionNueva!.ProductoPresentacionId
                        : null,

                    StockMinimo = stockMinimo
                };

                using var response = await _apiClient.Http.PostAsJsonAsync(
                    "api/inventario/articulos",
                    request);

                if (!response.IsSuccessStatusCode)
                {
                    Mensaje = await LeerErrorAsync(response);
                    return;
                }

                LimpiarArticulo();

                await CargarDatosAsync();

                Mensaje = "Artículo de inventario registrado correctamente.";
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

        private async Task RegistrarMermaAsync()
        {
            if (ArticuloMerma is null ||
                !IntentarDecimal(CantidadMermaTexto, out var cantidad) ||
                cantidad <= 0 ||
                cantidad != decimal.Round(cantidad, 3))
            {
                Mensaje = "Selecciona un artículo e introduce una cantidad válida.";
                return;
            }

            Ocupado = true;

            try
            {
                var request = new RegistrarMermaRequest
                {
                    ArticuloInventarioId =
                        ArticuloMerma.ArticuloInventarioId,

                    Cantidad = cantidad,
                    LoteInventarioId = null,
                    Motivo = MotivoMerma,

                    Observaciones = Limpiar(ObservacionesMerma)
                };

                using var response = await _apiClient.Http.PostAsJsonAsync(
                    "api/inventario/mermas",
                    request);

                if (!response.IsSuccessStatusCode)
                {
                    Mensaje = await LeerErrorAsync(response);
                    return;
                }

                LimpiarMerma();
                await CargarDatosAsync();

                Mensaje = "Merma registrada correctamente.";
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

        private async Task RegistrarAjusteAsync()
        {
            if (ArticuloAjuste is null ||
                !IntentarDecimal(CantidadContadaTexto, out var cantidad) ||
                cantidad < 0 ||
                cantidad != decimal.Round(cantidad, 3))
            {
                Mensaje = "Selecciona un artículo e introduce la cantidad contada correctamente.";
                return;
            }

            decimal? costo = null;

            if (!string.IsNullOrWhiteSpace(CostoEntradaTexto))
            {
                if (!IntentarDecimal(CostoEntradaTexto, out var valorCosto) ||
                    valorCosto < 0 ||
                    valorCosto != decimal.Round(valorCosto, 4))
                {
                    Mensaje = "El costo de entrada no es válido.";
                    return;
                }

                costo = valorCosto;
            }

            Ocupado = true;

            try
            {
                var request = new RegistrarAjusteInventarioRequest
                {
                    ArticuloInventarioId =
                        ArticuloAjuste.ArticuloInventarioId,

                    CantidadContada = cantidad,

                    Motivo = MotivoAjuste.Trim(),

                    CostoUnitarioEntrada = costo,

                    CodigoLote = Limpiar(CodigoLoteAjuste),

                    FechaVencimiento = FechaVencimientoAjuste.HasValue
                        ? DateOnly.FromDateTime(
                            FechaVencimientoAjuste.Value)
                        : null,

                    Observaciones = Limpiar(ObservacionesAjuste)
                };

                using var response = await _apiClient.Http.PostAsJsonAsync(
                    "api/inventario/ajustes",
                    request);

                if (!response.IsSuccessStatusCode)
                {
                    Mensaje = await LeerErrorAsync(response);
                    return;
                }

                LimpiarAjuste();
                await CargarDatosAsync();

                Mensaje = "Ajuste de inventario registrado correctamente.";
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

        private void LimpiarArticulo()
        {
            NombreArticulo = string.Empty;
            TipoArticulo = "Ingrediente";
            UnidadSeleccionada = null;
            PresentacionNueva = null;
            StockMinimoTexto = "0";
        }

        private void LimpiarMerma()
        {
            ArticuloMerma = null;
            CantidadMermaTexto = string.Empty;
            MotivoMerma = "Dañado";
            ObservacionesMerma = string.Empty;
        }

        private void LimpiarAjuste()
        {
            ArticuloAjuste = null;
            CantidadContadaTexto = string.Empty;
            MotivoAjuste = string.Empty;
            CostoEntradaTexto = string.Empty;
            CodigoLoteAjuste = string.Empty;
            FechaVencimientoAjuste = null;
            ObservacionesAjuste = string.Empty;
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

        private static bool IntentarDecimal(string texto, out decimal valor)
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

        private static bool EsDecimalNoNegativo(string texto)
        {
            return IntentarDecimal(texto, out var valor) &&
                   valor >= 0 &&
                   valor == decimal.Round(valor, 3);
        }

        private static string? Limpiar(string? valor)
        {
            return string.IsNullOrWhiteSpace(valor)
                ? null
                : valor.Trim();
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
