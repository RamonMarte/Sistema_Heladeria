using Heladeria.Desktop.ViewModels;
using System;
using System.Collections.Generic;
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
    /// Lógica de interacción para LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _viewModel;

        public LoginWindow(LoginViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            DataContext = _viewModel;

            _viewModel.LoginExitoso += AlIniciarSesion;
        }

        private void PasswordInput_PasswordChanged(
            object sender,
            RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm &&
                sender is PasswordBox passwordBox)
            {
                vm.Contrasena = passwordBox.Password;
            }
        }

        private void AlIniciarSesion()
        {
            var app = (App)Application.Current;

            var dashboard = new DashboardWindow(
                app.AuthService,
                app.SessionService);

            Application.Current.MainWindow = dashboard;

            dashboard.Show();
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.LoginExitoso -= AlIniciarSesion;
            base.OnClosed(e);
        }
    }
}
