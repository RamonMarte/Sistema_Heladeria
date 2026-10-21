using Heladeria.Contracts.Inventario;
using Heladeria.Contracts.Productos;
using Heladeria.Contracts.Recetas;
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
    public sealed class PresentacionRecetaOpcion
    {
        public int ProductoPresentacionId { get; set; }

        public string NombreCompleto { get; set; } = string.Empty;
    }

    public sealed class RecetasViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;
        

        private PresentacionRecetaOpcion? _presentacionSeleccionada;
        private ArticuloInventarioResponse? _articuloSeleccionado;
        private RecetaLineaViewModel? _lineaSeleccionada;

        private int? _recetaCargadaParaPresentacionId;
        private string _cantidadNuevaTexto = "1";
        private string _mensaje = string.Empty;
        private bool _ocupado;
        public bool PuedeSeleccionarPresentacion => !Ocupado;

        public ObservableCollection<PresentacionRecetaOpcion>
            Presentaciones
        { get; } = [];

        public ObservableCollection<ArticuloInventarioResponse>
            ArticulosDisponibles
        { get; } = [];

        public ObservableCollection<RecetaLineaViewModel>
            IngredientesReceta
        { get; } = [];

        public AsyncRelayCommand CargarCommand { get; }

        public AsyncRelayCommand CargarRecetaCommand { get; }

        public AsyncRelayCommand GuardarRecetaCommand { get; }

        public RelayCommand AgregarIngredienteCommand { get; }

        public RelayCommand QuitarIngredienteCommand { get; }

        public RecetasViewModel(ApiClient apiClient)
        {
            _apiClient = apiClient;

            CargarCommand = new AsyncRelayCommand(
                CargarDatosAsync);

            CargarRecetaCommand = new AsyncRelayCommand(
                CargarRecetaAsync,
                () => !Ocupado &&
                      PresentacionSeleccionada is not null);

            GuardarRecetaCommand = new AsyncRelayCommand(
                GuardarRecetaAsync,
                () => !Ocupado &&
                      PresentacionSeleccionada is not null &&
                      _recetaCargadaParaPresentacionId ==
                          PresentacionSeleccionada.ProductoPresentacionId &&
                      IngredientesReceta.Count > 0);

            AgregarIngredienteCommand = new RelayCommand(
                _ => AgregarIngrediente(),
                _ => !Ocupado &&
                     ArticuloSeleccionado is not null &&
                     EsCantidadValida(CantidadNuevaTexto));

            QuitarIngredienteCommand = new RelayCommand(
                _ => QuitarIngrediente(),
                _ => !Ocupado &&
                     LineaSeleccionada is not null);
        }

        public bool Ocupado
        {
            get => _ocupado;
            private set
            {
                if (SetProperty(ref _ocupado, value))
                {
                    CargarCommand.NotificarCambio();
                    CargarRecetaCommand.NotificarCambio();
                    GuardarRecetaCommand.NotificarCambio();
                    AgregarIngredienteCommand.NotificarCambio();
                    QuitarIngredienteCommand.NotificarCambio();
                    OnPropertyChanged(nameof(PuedeSeleccionarPresentacion));

                }

                


            }

           
        }

        public string Mensaje
        {
            get => _mensaje;
            private set => SetProperty(ref _mensaje, value);
        }

        public PresentacionRecetaOpcion? PresentacionSeleccionada
        {
            get => _presentacionSeleccionada;
            set
            {
                if (!SetProperty(ref _presentacionSeleccionada, value))
                    return;

                // La receta anterior no pertenece necesariamente
                // a la nueva presentación seleccionada.
                _recetaCargadaParaPresentacionId = null;

                IngredientesReceta.Clear();
                LineaSeleccionada = null;

                CargarRecetaCommand.NotificarCambio();
                GuardarRecetaCommand.NotificarCambio();

                Mensaje = value is null
                    ? "Selecciona una presentación."
                    : "Presentación seleccionada. Pulsa 'Cargar receta' antes de editar o guardar.";
            }
        }

        public ArticuloInventarioResponse? ArticuloSeleccionado
        {
            get => _articuloSeleccionado;
            set
            {
                if (SetProperty(ref _articuloSeleccionado, value))
                    AgregarIngredienteCommand.NotificarCambio();
            }
        }

        public RecetaLineaViewModel? LineaSeleccionada
        {
            get => _lineaSeleccionada;
            set
            {
                if (SetProperty(ref _lineaSeleccionada, value))
                    QuitarIngredienteCommand.NotificarCambio();
            }
        }

        public string CantidadNuevaTexto
        {
            get => _cantidadNuevaTexto;
            set
            {
                if (SetProperty(ref _cantidadNuevaTexto, value))
                    AgregarIngredienteCommand.NotificarCambio();
            }
        }

        public Task InicializarAsync() => CargarDatosAsync();

        private async Task CargarDatosAsync()
        {
            if (Ocupado)
                return;

            try
            {
                Ocupado = true;
                Mensaje = "Cargando presentaciones e inventario...";

                int? presentacionAnterior =
                    PresentacionSeleccionada?.ProductoPresentacionId;

                var productos =
                    await ObtenerJsonAsync<List<ProductoResponse>>(
                        "api/productos?incluirInactivos=false");

                var articulos =
                    await ObtenerJsonAsync<List<ArticuloInventarioResponse>>(
                        "api/inventario/articulos?incluirInactivos=false");

                Presentaciones.Clear();

                foreach (var producto in productos)
                {
                    foreach (var presentacion in producto.Presentaciones
                                 .Where(p => p.Activa)
                                 .OrderBy(p => p.NombrePresentacion))
                    {
                        Presentaciones.Add(
                            new PresentacionRecetaOpcion
                            {
                                ProductoPresentacionId =
                                    presentacion.ProductoPresentacionId,

                                NombreCompleto =
                                    $"{producto.Nombre} - " +
                                    presentacion.NombrePresentacion
                            });
                    }
                }

                ArticulosDisponibles.Clear();

                foreach (var articulo in articulos
                             .Where(a =>
                                 a.Activo &&
                                 (a.TipoArticulo == "Ingrediente" ||
                                  a.TipoArticulo == "Material"))
                             .OrderBy(a => a.Nombre))
                {
                    ArticulosDisponibles.Add(articulo);
                }

                PresentacionSeleccionada =
                    Presentaciones.FirstOrDefault(p =>
                        p.ProductoPresentacionId == presentacionAnterior)
                    ?? Presentaciones.FirstOrDefault();

                IngredientesReceta.Clear();
                LineaSeleccionada = null;

                GuardarRecetaCommand.NotificarCambio();

                Mensaje =
                    $"Presentaciones: {Presentaciones.Count}. " +
                    $"Ingredientes y materiales disponibles: " +
                    $"{ArticulosDisponibles.Count}.";
            }
            catch (Exception ex)
            {
                Mensaje = $"No se pudieron cargar los datos: {ex.Message}";
            }
            finally
            {
                Ocupado = false;
            }
        }

        private async Task CargarRecetaAsync()
        {
            if (Ocupado || PresentacionSeleccionada is null)
                return;

            try
            {
                Ocupado = true;
                Mensaje = "Consultando receta...";

                var receta = await ObtenerJsonAsync<RecetaResponse>(
                    $"api/recetas/{PresentacionSeleccionada.ProductoPresentacionId}");

                IngredientesReceta.Clear();

                foreach (var ingrediente in receta.Ingredientes)
                {
                    IngredientesReceta.Add(
                        new RecetaLineaViewModel
                        {
                            ArticuloInventarioId =
                                ingrediente.ArticuloInventarioId,

                            NombreArticulo =
                                ingrediente.NombreArticulo,

                            TipoArticulo =
                                ingrediente.TipoArticulo,

                            UnidadMedida =
                                ingrediente.UnidadMedida,

                            Abreviatura =
                                ingrediente.Abreviatura,

                            CantidadTexto =
                                ingrediente.CantidadPorUnidad.ToString(
                                    "0.####",
                                    CultureInfo.InvariantCulture)
                        });
                }

                LineaSeleccionada = null;

                GuardarRecetaCommand.NotificarCambio();

                Mensaje = receta.Ingredientes.Count == 0
                    ? $"La presentación {receta.Producto} - " +
                      $"{receta.Presentacion} todavía no tiene receta."
                    : $"Receta cargada: {receta.Ingredientes.Count} componentes.";
            }
            catch (Exception ex)
            {
                Mensaje = $"No se pudo cargar la receta: {ex.Message}";
            }
            finally
            {
                Ocupado = false;
            }
        }

        private void AgregarIngrediente()
        {
            if (ArticuloSeleccionado is null)
            {
                Mensaje = "Selecciona un ingrediente o material.";
                return;
            }

            if (!IntentarCantidad(
                    CantidadNuevaTexto,
                    out decimal cantidad))
            {
                Mensaje =
                    "La cantidad debe ser positiva y admitir hasta cuatro decimales.";
                return;
            }

            if (IngredientesReceta.Any(i =>
                    i.ArticuloInventarioId ==
                    ArticuloSeleccionado.ArticuloInventarioId))
            {
                Mensaje =
                    "Ese artículo ya está en la receta. " +
                    "Modifica su cantidad en la tabla.";
                return;
            }

            IngredientesReceta.Add(
                new RecetaLineaViewModel
                {
                    ArticuloInventarioId =
                        ArticuloSeleccionado.ArticuloInventarioId,

                    NombreArticulo = ArticuloSeleccionado.Nombre,

                    TipoArticulo = ArticuloSeleccionado.TipoArticulo,

                    UnidadMedida = ArticuloSeleccionado.UnidadMedida,

                    Abreviatura = ArticuloSeleccionado.Abreviatura,

                    CantidadTexto = cantidad.ToString(
                        "0.####",
                        CultureInfo.InvariantCulture)
                });

            ArticuloSeleccionado = null;
            CantidadNuevaTexto = "1";

            GuardarRecetaCommand.NotificarCambio();

            Mensaje =
                "Componente agregado. Recuerda guardar la receta.";
        }

        private void QuitarIngrediente()
        {
            if (LineaSeleccionada is null)
                return;

            IngredientesReceta.Remove(LineaSeleccionada);

            LineaSeleccionada = null;

            GuardarRecetaCommand.NotificarCambio();

            Mensaje =
                "Componente quitado de la edición. " +
                "Guarda la receta para confirmar los cambios.";
        }

        private async Task GuardarRecetaAsync()
        {
            if (Ocupado || PresentacionSeleccionada is null)
                return;

            if (IngredientesReceta.Count == 0)
            {
                Mensaje =
                    "La API exige al menos un ingrediente o material por receta.";
                return;
            }

            if (IngredientesReceta.Count > 50)
            {
                Mensaje = "Una receta no puede tener más de 50 componentes.";
                return;
            }

            var ingredientes = new List<RecetaIngredienteRequest>();

            foreach (var linea in IngredientesReceta)
            {
                if (!IntentarCantidad(
                        linea.CantidadTexto,
                        out decimal cantidad))
                {
                    Mensaje =
                        $"La cantidad de {linea.NombreArticulo} no es válida.";
                    return;
                }

                ingredientes.Add(
                    new RecetaIngredienteRequest
                    {
                        ArticuloInventarioId =
                            linea.ArticuloInventarioId,

                        CantidadPorUnidad = cantidad
                    });
            }

            try
            {
                Ocupado = true;
                Mensaje = "Guardando receta...";

                var request = new ActualizarRecetaRequest
                {
                    Ingredientes = ingredientes
                };

                using var respuesta =
                    await _apiClient.Http.PutAsJsonAsync(
                        $"api/recetas/{PresentacionSeleccionada.ProductoPresentacionId}",
                        request);

                if (!respuesta.IsSuccessStatusCode)
                {
                    Mensaje = await LeerErrorAsync(respuesta);
                    return;
                }

                await CargarRecetaAsyncInterna();

                Mensaje = "Receta guardada correctamente.";
            }
            catch (Exception ex)
            {
                Mensaje = $"No se pudo guardar la receta: {ex.Message}";
            }
            finally
            {
                Ocupado = false;
            }
        }

        private async Task CargarRecetaAsyncInterna()
        {
            if (PresentacionSeleccionada is null)
                return;

            var receta = await ObtenerJsonAsync<RecetaResponse>(
                $"api/recetas/{PresentacionSeleccionada.ProductoPresentacionId}");

            IngredientesReceta.Clear();

            foreach (var ingrediente in receta.Ingredientes)
            {
                IngredientesReceta.Add(
                    new RecetaLineaViewModel
                    {
                        ArticuloInventarioId =
                            ingrediente.ArticuloInventarioId,

                        NombreArticulo =
                            ingrediente.NombreArticulo,

                        TipoArticulo =
                            ingrediente.TipoArticulo,

                        UnidadMedida =
                            ingrediente.UnidadMedida,

                        Abreviatura =
                            ingrediente.Abreviatura,

                        CantidadTexto =
                            ingrediente.CantidadPorUnidad.ToString(
                                "0.####",
                                CultureInfo.InvariantCulture)
                    });
            }

            GuardarRecetaCommand.NotificarCambio();
        }

        private async Task<T> ObtenerJsonAsync<T>(string ruta)
        {
            using var respuesta = await _apiClient.Http.GetAsync(ruta);

            if (!respuesta.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    await LeerErrorAsync(respuesta));
            }

            return await respuesta.Content.ReadFromJsonAsync<T>()
                ?? throw new InvalidOperationException(
                    "La API devolvió una respuesta vacía.");
        }

        private static async Task<string> LeerErrorAsync(
            HttpResponseMessage respuesta)
        {
            string contenido =
                await respuesta.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(contenido))
                return $"Error HTTP {(int)respuesta.StatusCode}.";

            try
            {
                using var documento = JsonDocument.Parse(contenido);
                var raiz = documento.RootElement;

                if (raiz.TryGetProperty("mensaje", out var mensaje))
                    return mensaje.GetString() ?? contenido;

                if (raiz.TryGetProperty("detail", out var detalle))
                    return detalle.GetString() ?? contenido;

                if (raiz.TryGetProperty("title", out var titulo))
                    return titulo.GetString() ?? contenido;
            }
            catch (JsonException)
            {
                // La API devolvió texto sin formato JSON.
            }

            return contenido;
        }

        private static bool EsCantidadValida(string texto)
        {
            return IntentarCantidad(texto, out _);
        }

        private static bool IntentarCantidad(
            string texto,
            out decimal cantidad)
        {
            bool leido =
                decimal.TryParse(
                    texto,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out cantidad)
                ||
                decimal.TryParse(
                    texto,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out cantidad);

            return leido &&
                   cantidad > 0 &&
                   cantidad == decimal.Round(cantidad, 4);
        }
    }
}
