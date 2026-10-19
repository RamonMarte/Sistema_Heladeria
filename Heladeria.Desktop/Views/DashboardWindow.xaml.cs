using Heladeria.Desktop.Services;
using Heladeria.Desktop.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Heladeria.Desktop.Views
{
    /// <summary>
    /// Lógica de interacción para DashboardWindow.xaml
    /// </summary>
    public partial class DashboardWindow : Window
    {
        private readonly AuthService _authService;
        private readonly SessionService _sessionService;

        private bool _permitirCierre;
        private bool _cerrandoSesion;

        public DashboardWindow(
            AuthService authService,
            SessionService sessionService)
        {
            InitializeComponent();

            _authService = authService;
            _sessionService = sessionService;

            var usuario = _sessionService.UsuarioActual;

            TextoBienvenida.Text =
                $"Bienvenido, {usuario?.NombreCompleto ?? "Usuario"}";

            TextoRol.Text =
                $"Usuario: {usuario?.NombreUsuario} | Rol: {usuario?.Rol}";

            TextoAdministracion.Visibility =
                _sessionService.EsAdministrador
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            TarjetaAdministracion.Visibility =
                _sessionService.EsAdministrador
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private async void CerrarSesion_Click(
            object sender,
            RoutedEventArgs e)
        {
            await CerrarSesionYVolverAsync();
        }

        private async Task CerrarSesionYVolverAsync()
        {
            try
            {
                await _authService.CerrarSesionAsync();

                var app = (App)Application.Current;

                var login = new LoginWindow(
                    new LoginViewModel(_authService));

                Application.Current.MainWindow = login;
                login.Show();

                _permitirCierre = true;
                Close();
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "No se pudo registrar el cierre de sesión en el servidor. " +
                    "Comprueba la conexión con la API e inténtalo nuevamente.",
                    "No se pudo cerrar sesión",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private async void Window_Closing(
            object? sender,
            CancelEventArgs e)
        {
            if (_permitirCierre || !_sessionService.EstaAutenticado)
                return;

            e.Cancel = true;

            if (_cerrandoSesion)
                return;

            _cerrandoSesion = true;

            try
            {
                await _authService.CerrarSesionAsync();

                _permitirCierre = true;
                Application.Current.Shutdown();
            }
            catch
            {
                _cerrandoSesion = false;

                MessageBox.Show(
                    "No se pudo registrar el cierre de sesión. " +
                    "Mantendremos la aplicación abierta para que puedas reintentarlo.",
                    "Error al cerrar sesión",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void AbrirPuntoVenta_Click(
            object sender,
            RoutedEventArgs e)
        {
            var app = (App)Application.Current;

            var ventana = new PuntoVentaWindow(
                app.ApiClient,
                this);

            // Abrir el punto de venta.
            ventana.Show();
            ventana.Activate();

            // Minimizar el menú principal.
            WindowState = WindowState.Minimized;
        }
    }
}
