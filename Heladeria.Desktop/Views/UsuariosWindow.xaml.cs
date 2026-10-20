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
    /// Lógica de interacción para UsuariosWindow.xaml
    /// </summary>
    public partial class UsuariosWindow : Window
    {
        private readonly UsuariosViewModel _viewModel;
        private readonly Window _ventanaAnterior;

        public UsuariosWindow(
            ApiClient apiClient,
            Window ventanaAnterior)
        {
            InitializeComponent();

            _ventanaAnterior = ventanaAnterior;

            _viewModel = new UsuariosViewModel(apiClient);

            DataContext = _viewModel;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            Closed += UsuariosWindow_Closed;
        }

        private async void Window_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            await _viewModel.InicializarAsync();
        }

        private void PasswordCreacion_PasswordChanged(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                _viewModel.NuevaContrasena =
                    passwordBox.Password;
            }
        }

        private void PasswordRestablecer_PasswordChanged(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                _viewModel.ContrasenaRestablecer =
                    passwordBox.Password;
            }
        }

        private void ViewModel_PropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName ==
                    nameof(UsuariosViewModel.NuevaContrasena) &&
                string.IsNullOrEmpty(_viewModel.NuevaContrasena))
            {
                PasswordCreacion.Clear();
            }

            if (e.PropertyName ==
                    nameof(UsuariosViewModel.ContrasenaRestablecer) &&
                string.IsNullOrEmpty(_viewModel.ContrasenaRestablecer))
            {
                PasswordRestablecer.Clear();
            }
        }

        private void Volver_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        private void UsuariosWindow_Closed(
            object? sender,
            EventArgs e)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;

            if (!_ventanaAnterior.IsLoaded)
                return;

            _ventanaAnterior.WindowState = WindowState.Normal;
            _ventanaAnterior.Show();
            _ventanaAnterior.Activate();
        }
    }
}
