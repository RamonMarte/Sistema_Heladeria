using Heladeria.Contracts.Cajas;
using Heladeria.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows.Input;

namespace Heladeria.Desktop.ViewModels
{
    public sealed class CajasViewModel : INotifyPropertyChanged
    {
        private readonly ApiClient _apiClient;

        private readonly AsyncCommand _cargarCommand;
        private readonly AsyncCommand _guardarCommand;

        private CajaResponse? _cajaSeleccionada;
        private string _codigo = string.Empty;
        private string _nombre = string.Empty;
        private bool _activa = true;
        private bool _ocupado;
        private string _mensaje = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public CajasViewModel(ApiClient apiClient)
        {
            _apiClient = apiClient;

            _cargarCommand = new AsyncCommand(
                CargarAsync,
                () => !Ocupado);

            _guardarCommand = new AsyncCommand(
                GuardarAsync,
                () => !Ocupado);

            CargarCommand = _cargarCommand;
            GuardarCommand = _guardarCommand;

            NuevoCommand = new ActionCommand(NuevaCaja);
        }

        public ObservableCollection<CajaResponse> Cajas { get; } = new();

        public ICommand CargarCommand { get; }

        public ICommand GuardarCommand { get; }

        public ICommand NuevoCommand { get; }

        public string Codigo
        {
            get => _codigo;
            set => Cambiar(ref _codigo, value);
        }

        public string Nombre
        {
            get => _nombre;
            set => Cambiar(ref _nombre, value);
        }

        public bool Activa
        {
            get => _activa;
            set => Cambiar(ref _activa, value);
        }

        public bool Ocupado
        {
            get => _ocupado;
            private set
            {
                if (Cambiar(ref _ocupado, value))
                {
                    _cargarCommand.ActualizarEstado();
                    _guardarCommand.ActualizarEstado();
                }
            }
        }

        public string Mensaje
        {
            get => _mensaje;
            private set => Cambiar(ref _mensaje, value);
        }

        public CajaResponse? CajaSeleccionada
        {
            get => _cajaSeleccionada;
            set
            {
                if (!Cambiar(ref _cajaSeleccionada, value))
                    return;

                if (value is not null)
                {
                    Codigo = value.Codigo;
                    Nombre = value.Nombre;
                    Activa = value.Activa;
                }

                OnPropertyChanged(nameof(EsEdicion));
                OnPropertyChanged(nameof(TextoBotonGuardar));
            }
        }

        public bool EsEdicion => CajaSeleccionada is not null;

        public string TextoBotonGuardar =>
            EsEdicion ? "Guardar cambios" : "Crear caja";

        private async Task CargarAsync()
        {
            if (Ocupado)
                return;

            try
            {
                Ocupado = true;
                Mensaje = "Cargando cajas...";

                await CargarListadoAsync();

                Mensaje = $"Se encontraron {Cajas.Count} cajas.";
            }
            catch (Exception ex)
            {
                Mensaje = $"No se pudieron cargar las cajas: {ex.Message}";
            }
            finally
            {
                Ocupado = false;
            }
        }

        private async Task CargarListadoAsync()
        {
            using HttpResponseMessage respuesta =
                await _apiClient.Http.GetAsync(
                    "api/cajas?incluirInactivas=true");

            if (!respuesta.IsSuccessStatusCode)
            {
                string error = await LeerErrorAsync(respuesta);

                throw new HttpRequestException(
                    $"Error {(int)respuesta.StatusCode}: {error}");
            }

            var cajas = await respuesta.Content
                .ReadFromJsonAsync<List<CajaResponse>>()
                ?? new List<CajaResponse>();

            Cajas.Clear();

            foreach (var caja in cajas)
                Cajas.Add(caja);
        }

        private async Task GuardarAsync()
        {
            if (Ocupado)
                return;

            Codigo = Codigo.Trim();
            Nombre = Nombre.Trim();

            if (Codigo.Length < 2 || Codigo.Length > 30)
            {
                Mensaje = "El código debe contener entre 2 y 30 caracteres.";
                return;
            }

            if (Nombre.Length < 2 || Nombre.Length > 80)
            {
                Mensaje = "El nombre debe contener entre 2 y 80 caracteres.";
                return;
            }

            bool esNueva = CajaSeleccionada is null;

            try
            {
                Ocupado = true;
                Mensaje = esNueva
                    ? "Creando caja..."
                    : "Guardando cambios...";

                using HttpResponseMessage respuesta = esNueva
                    ? await _apiClient.Http.PostAsJsonAsync(
                        "api/cajas",
                        new CrearCajaRequest
                        {
                            Codigo = Codigo,
                            Nombre = Nombre
                        })
                    : await _apiClient.Http.PutAsJsonAsync(
                        $"api/cajas/{CajaSeleccionada!.CajaId}",
                        new ActualizarCajaRequest
                        {
                            Codigo = Codigo,
                            Nombre = Nombre,
                            Activa = Activa
                        });

                if (!respuesta.IsSuccessStatusCode)
                {
                    string error = await LeerErrorAsync(respuesta);

                    Mensaje = $"No se pudo guardar la caja: {error}";
                    return;
                }

                await CargarListadoAsync();

                NuevaCaja();

                Mensaje = esNueva
                    ? "Caja creada correctamente."
                    : "Caja actualizada correctamente.";
            }
            catch (Exception ex)
            {
                Mensaje = $"Ocurrió un error: {ex.Message}";
            }
            finally
            {
                Ocupado = false;
            }
        }

        private void NuevaCaja()
        {
            CajaSeleccionada = null;
            Codigo = string.Empty;
            Nombre = string.Empty;
            Activa = true;

            OnPropertyChanged(nameof(EsEdicion));
            OnPropertyChanged(nameof(TextoBotonGuardar));
        }

        private static async Task<string> LeerErrorAsync(
            HttpResponseMessage respuesta)
        {
            string contenido =
                await respuesta.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(contenido))
                return respuesta.ReasonPhrase ?? "Error desconocido.";

            try
            {
                using JsonDocument documento =
                    JsonDocument.Parse(contenido);

                JsonElement raiz = documento.RootElement;

                if (raiz.TryGetProperty("mensaje", out var mensaje))
                    return mensaje.GetString() ?? contenido;

                if (raiz.TryGetProperty("detail", out var detalle))
                    return detalle.GetString() ?? contenido;

                if (raiz.TryGetProperty("title", out var titulo))
                    return titulo.GetString() ?? contenido;
            }
            catch (JsonException)
            {
                // La API devolvió texto que no está en formato JSON.
            }

            return contenido;
        }

        private bool Cambiar<T>(
            ref T campo,
            T valor,
            [CallerMemberName] string? propiedad = null)
        {
            if (EqualityComparer<T>.Default.Equals(campo, valor))
                return false;

            campo = valor;
            OnPropertyChanged(propiedad);
            return true;
        }

        private void OnPropertyChanged(
            [CallerMemberName] string? propiedad = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propiedad));
        }

        private sealed class ActionCommand : ICommand
        {
            private readonly Action _ejecutar;

            public ActionCommand(Action ejecutar)
            {
                _ejecutar = ejecutar;
            }

            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter) => true;

            public void Execute(object? parameter) => _ejecutar();
        }

        private sealed class AsyncCommand : ICommand
        {
            private readonly Func<Task> _ejecutar;
            private readonly Func<bool> _puedeEjecutar;

            private bool _ejecutando;

            public AsyncCommand(
                Func<Task> ejecutar,
                Func<bool> puedeEjecutar)
            {
                _ejecutar = ejecutar;
                _puedeEjecutar = puedeEjecutar;
            }

            public event EventHandler? CanExecuteChanged;

            public bool CanExecute(object? parameter) =>
                !_ejecutando && _puedeEjecutar();

            public async void Execute(object? parameter)
            {
                if (!CanExecute(parameter))
                    return;

                _ejecutando = true;
                ActualizarEstado();

                try
                {
                    await _ejecutar();
                }
                finally
                {
                    _ejecutando = false;
                    ActualizarEstado();
                }
            }

            public void ActualizarEstado()
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
