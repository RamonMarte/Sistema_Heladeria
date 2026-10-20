using Heladeria.Desktop.Services;
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
    /// Lógica de interacción para InventarioWindow.xaml
    /// </summary>
    public partial class InventarioWindow : Window
    {
        private readonly InventarioViewModel _viewModel;
        private readonly Window _ventanaAnterior;

        public InventarioWindow(
            ApiClient apiClient,
            Window ventanaAnterior)
        {
            InitializeComponent();

            _ventanaAnterior = ventanaAnterior;

            _viewModel = new InventarioViewModel(apiClient);

            DataContext = _viewModel;

            Closed += InventarioWindow_Closed;
        }

        private async void Window_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            await _viewModel.InicializarAsync();
        }

        private void Volver_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        private void InventarioWindow_Closed(
            object? sender,
            EventArgs e)
        {
            if (!_ventanaAnterior.IsLoaded)
                return;

            _ventanaAnterior.WindowState = WindowState.Normal;
            _ventanaAnterior.Show();
            _ventanaAnterior.Activate();
        }
    }
}
