using Heladeria.Contracts.Auth;
using Heladeria.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Heladeria.Desktop.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly AuthService _authService;

        private string _nombreUsuario = string.Empty;
        private string _contrasena = string.Empty;
        private string _mensajeError = string.Empty;
        private bool _estaOcupado;

        public string NombreUsuario
        {
            get => _nombreUsuario;
            set
            {
                if (SetProperty(ref _nombreUsuario, value))
                    LoginCommand.NotificarCambio();
            }
        }

        public string Contrasena
        {
            get => _contrasena;
            set
            {
                if (SetProperty(ref _contrasena, value))
                    LoginCommand.NotificarCambio();
            }
        }

        public string MensajeError
        {
            get => _mensajeError;
            private set => SetProperty(ref _mensajeError, value);
        }

        public bool EstaOcupado
        {
            get => _estaOcupado;
            private set
            {
                if (SetProperty(ref _estaOcupado, value))
                    LoginCommand.NotificarCambio();
            }
        }

        public AsyncRelayCommand LoginCommand { get; }

        public event Action? LoginExitoso;

        public LoginViewModel(AuthService authService)
        {
            _authService = authService;

            LoginCommand = new AsyncRelayCommand(
                IniciarSesionAsync,
                () => !EstaOcupado &&
                      !string.IsNullOrWhiteSpace(NombreUsuario) &&
                      !string.IsNullOrWhiteSpace(Contrasena));
        }

        private async Task IniciarSesionAsync()
        {
            EstaOcupado = true;
            MensajeError = string.Empty;

            try
            {
                var solicitud = new LoginRequest
                {
                    NombreUsuario = NombreUsuario.Trim(),
                    Contrasena = Contrasena,
                    Equipo = Environment.MachineName
                };

                await _authService.IniciarSesionAsync(solicitud);

                Contrasena = string.Empty;

                LoginExitoso?.Invoke();
            }
            catch (InvalidOperationException ex)
            {
                MensajeError = ex.Message;
            }
            catch (HttpRequestException)
            {
                MensajeError =
                    "No se pudo conectar con la API. Comprueba que esté ejecutándose.";
            }
            catch (TaskCanceledException)
            {
                MensajeError =
                    "La solicitud tardó demasiado. Inténtalo nuevamente.";
            }
            catch
            {
                MensajeError =
                    "Ocurrió un error al iniciar sesión. Revisa la API.";
            }
            finally
            {
                EstaOcupado = false;
            }
        }
    }
}
