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
    /// Lógica de interacción para ComprasWindow.xaml
    /// </summary>
    public partial class ComprasWindow : Window
    {
        private readonly ComprasViewModel _viewModel;
        private readonly Window _ventanaAnterior;

        public ComprasWindow(ApiClient apiClient, Window ventanaAnterior)
        {
            InitializeComponent();

            _ventanaAnterior = ventanaAnterior;
            _viewModel = new ComprasViewModel(apiClient);

            DataContext = _viewModel;

            Closed += ComprasWindow_Closed;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InicializarAsync();
        }

        private void Volver_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ComprasWindow_Closed(object? sender, EventArgs e)
        {
            if (!_ventanaAnterior.IsLoaded)
                return;

            _ventanaAnterior.WindowState = WindowState.Normal;
            _ventanaAnterior.Show();
            _ventanaAnterior.Activate();
        }
    }
}
