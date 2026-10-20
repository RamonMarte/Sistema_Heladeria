using Heladeria.Contracts.Usuarios;
using Heladeria.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Heladeria.Desktop.ViewModels
{
    public class UsuariosViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;
        private List<UsuarioResponse> _todosUsuarios = [];

        private string _buscar = string.Empty;
        private string _mensaje = string.Empty;

        private string _nuevoNombreCompleto = string.Empty;
        private string _nuevoNombreUsuario = string.Empty;
        private string _nuevaContrasena = string.Empty;
        private string _nuevoRol = "Cajero";

        private UsuarioResponse? _usuarioSeleccionado;
        private string _nombreCompletoEdicion = string.Empty;
        private string _rolEdicion = "Cajero";
        private bool _activoEdicion = true;
        private string _contrasenaRestablecer = string.Empty;

        public ObservableCollection<UsuarioResponse> Usuarios { get; } = [];

        public ObservableCollection<string> Roles { get; } =
            ["Encargado", "Cajero"];

        public AsyncRelayCommand CargarCommand { get; }
        public AsyncRelayCommand CrearCommand { get; }
        public AsyncRelayCommand GuardarCambiosCommand { get; }
        public AsyncRelayCommand RestablecerContrasenaCommand { get; }

        public string Buscar
        {
            get => _buscar;
            set
            {
                if (SetProperty(ref _buscar, value))
                    AplicarFiltro();
            }
        }

        public string Mensaje
        {
            get => _mensaje;
            private set => SetProperty(ref _mensaje, value);
        }

        public string NuevoNombreCompleto
        {
            get => _nuevoNombreCompleto;
            set
            {
                if (SetProperty(ref _nuevoNombreCompleto, value))
                    CrearCommand.NotificarCambio();
            }
        }

        public string NuevoNombreUsuario
        {
            get => _nuevoNombreUsuario;
            set
            {
                if (SetProperty(ref _nuevoNombreUsuario, value))
                    CrearCommand.NotificarCambio();
            }
        }

        public string NuevaContrasena
        {
            get => _nuevaContrasena;
            set
            {
                if (SetProperty(ref _nuevaContrasena, value))
                    CrearCommand.NotificarCambio();
            }
        }

        public string NuevoRol
        {
            get => _nuevoRol;
            set => SetProperty(ref _nuevoRol, value);
        }

        public UsuarioResponse? UsuarioSeleccionado
        {
            get => _usuarioSeleccionado;
            set
            {
                if (!SetProperty(ref _usuarioSeleccionado, value))
                    return;

                NombreCompletoEdicion =
                    value?.NombreCompleto ?? string.Empty;

                RolEdicion =
                    value?.Rol is "Encargado" or "Cajero"
                        ? value.Rol
                        : "Cajero";

                ActivoEdicion = value?.Activo ?? true;
                ContrasenaRestablecer = string.Empty;

                OnPropertyChanged(nameof(PuedeEditarUsuario));

                GuardarCambiosCommand.NotificarCambio();
                RestablecerContrasenaCommand.NotificarCambio();
            }
        }

        public bool PuedeEditarUsuario =>
            UsuarioSeleccionado is not null &&
            UsuarioSeleccionado.Rol is "Encargado" or "Cajero";

        public string NombreCompletoEdicion
        {
            get => _nombreCompletoEdicion;
            set
            {
                if (SetProperty(ref _nombreCompletoEdicion, value))
                    GuardarCambiosCommand.NotificarCambio();
            }
        }

        public string RolEdicion
        {
            get => _rolEdicion;
            set
            {
                if (SetProperty(ref _rolEdicion, value))
                    GuardarCambiosCommand.NotificarCambio();
            }
        }

        public bool ActivoEdicion
        {
            get => _activoEdicion;
            set
            {
                if (SetProperty(ref _activoEdicion, value))
                    GuardarCambiosCommand.NotificarCambio();
            }
        }

        public string ContrasenaRestablecer
        {
            get => _contrasenaRestablecer;
            set
            {
                if (SetProperty(ref _contrasenaRestablecer, value))
                    RestablecerContrasenaCommand.NotificarCambio();
            }
        }

        public UsuariosViewModel(ApiClient apiClient)
        {
            _apiClient = apiClient;

            CargarCommand = new AsyncRelayCommand(CargarAsync);

            CrearCommand = new AsyncRelayCommand(
                CrearAsync,
                () =>
                    !string.IsNullOrWhiteSpace(NuevoNombreCompleto) &&
                    !string.IsNullOrWhiteSpace(NuevoNombreUsuario) &&
                    NuevaContrasena.Length >= 12);

            GuardarCambiosCommand = new AsyncRelayCommand(
                GuardarCambiosAsync,
                () =>
                    PuedeEditarUsuario &&
                    !string.IsNullOrWhiteSpace(NombreCompletoEdicion) &&
                    RolEdicion is "Encargado" or "Cajero");

            RestablecerContrasenaCommand = new AsyncRelayCommand(
                RestablecerContrasenaAsync,
                () =>
                    PuedeEditarUsuario &&
                    ContrasenaRestablecer.Length >= 12);
        }

        public async Task InicializarAsync()
        {
            await CargarAsync();
        }

        private async Task CargarAsync()
        {
            try
            {
                using var response = await _apiClient.Http.GetAsync(
                    "api/usuarios?incluirInactivos=true");

                if (!response.IsSuccessStatusCode)
                {
                    Mensaje = await LeerErrorAsync(response);
                    return;
                }

                _todosUsuarios =
                    await response.Content.ReadFromJsonAsync<
                        List<UsuarioResponse>>() ?? [];

                AplicarFiltro();

                Mensaje = $"Usuarios registrados: {_todosUsuarios.Count}.";
            }
            catch (HttpRequestException)
            {
                Mensaje = "No se pudo conectar con la API.";
            }
            catch (TaskCanceledException)
            {
                Mensaje = "La consulta tardó demasiado.";
            }
        }

        private void AplicarFiltro()
        {
            var texto = Buscar.Trim();

            var resultado = string.IsNullOrWhiteSpace(texto)
                ? _todosUsuarios
                : _todosUsuarios.Where(u =>
                    u.NombreCompleto.Contains(
                        texto, StringComparison.OrdinalIgnoreCase) ||
                    u.NombreUsuario.Contains(
                        texto, StringComparison.OrdinalIgnoreCase) ||
                    u.Rol.Contains(
                        texto, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            var idSeleccionado = UsuarioSeleccionado?.UsuarioId;

            Usuarios.Clear();

            foreach (var usuario in resultado.OrderBy(u => u.NombreCompleto))
                Usuarios.Add(usuario);

            if (idSeleccionado.HasValue)
            {
                UsuarioSeleccionado = Usuarios.FirstOrDefault(
                    u => u.UsuarioId == idSeleccionado.Value);
            }
        }

        private async Task CrearAsync()
        {
            var nombre = NuevoNombreCompleto.Trim();
            var usuario = NuevoNombreUsuario.Trim();

            if (NuevaContrasena.Length < 12)
            {
                Mensaje = "La contraseña debe tener al menos 12 caracteres.";
                return;
            }

            if (NuevoRol is not "Encargado" and not "Cajero")
            {
                Mensaje = "Selecciona un rol válido.";
                return;
            }

            try
            {
                var request = new CrearUsuarioRequest
                {
                    NombreCompleto = nombre,
                    NombreUsuario = usuario,
                    Contrasena = NuevaContrasena,
                    Rol = NuevoRol
                };

                using var response = await _apiClient.Http.PostAsJsonAsync(
                    "api/usuarios",
                    request);

                if (!response.IsSuccessStatusCode)
                {
                    Mensaje = await LeerErrorAsync(response);
                    return;
                }

                NuevoNombreCompleto = string.Empty;
                NuevoNombreUsuario = string.Empty;
                NuevaContrasena = string.Empty;
                NuevoRol = "Cajero";

                await CargarAsync();

                Mensaje = $"Usuario '{usuario}' creado correctamente.";
            }
            catch (HttpRequestException)
            {
                Mensaje = "No se pudo conectar con la API.";
            }
            catch (TaskCanceledException)
            {
                Mensaje = "La operación tardó demasiado. Verifica los usuarios antes de repetirla.";
            }
        }

        private async Task GuardarCambiosAsync()
        {
            var seleccionado = UsuarioSeleccionado;

            if (seleccionado is null || !PuedeEditarUsuario)
                return;

            try
            {
                var request = new ActualizarUsuarioRequest
                {
                    NombreCompleto = NombreCompletoEdicion.Trim(),
                    Rol = RolEdicion,
                    Activo = ActivoEdicion
                };

                using var response = await _apiClient.Http.PutAsJsonAsync(
                    $"api/usuarios/{seleccionado.UsuarioId}",
                    request);

                if (!response.IsSuccessStatusCode)
                {
                    Mensaje = await LeerErrorAsync(response);
                    return;
                }

                var id = seleccionado.UsuarioId;

                await CargarAsync();

                UsuarioSeleccionado = Usuarios.FirstOrDefault(
                    u => u.UsuarioId == id);

                Mensaje = "Los datos del usuario se actualizaron correctamente.";
            }
            catch (HttpRequestException)
            {
                Mensaje = "No se pudo conectar con la API.";
            }
            catch (TaskCanceledException)
            {
                Mensaje = "La operación tardó demasiado. Comprueba el estado del usuario.";
            }
        }

        private async Task RestablecerContrasenaAsync()
        {
            var seleccionado = UsuarioSeleccionado;

            if (seleccionado is null || !PuedeEditarUsuario)
                return;

            if (ContrasenaRestablecer.Length < 12)
            {
                Mensaje = "La nueva contraseña debe tener al menos 12 caracteres.";
                return;
            }

            try
            {
                var request = new CambiarContrasenaUsuarioRequest
                {
                    NuevaContrasena = ContrasenaRestablecer
                };

                using var response = await _apiClient.Http.PutAsJsonAsync(
                    $"api/usuarios/{seleccionado.UsuarioId}/contrasena",
                    request);

                if (!response.IsSuccessStatusCode)
                {
                    Mensaje = await LeerErrorAsync(response);
                    return;
                }

                ContrasenaRestablecer = string.Empty;

                Mensaje =
                    "Contraseña restablecida. El empleado deberá iniciar sesión nuevamente.";
            }
            catch (HttpRequestException)
            {
                Mensaje = "No se pudo conectar con la API.";
            }
            catch (TaskCanceledException)
            {
                Mensaje = "La operación tardó demasiado. Comprueba el estado de la cuenta.";
            }
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
            }
            catch (JsonException)
            {
                // La API respondió con contenido que no es JSON.
            }

            return $"La API respondió con el código {(int)response.StatusCode}.";
        }
    }
}
