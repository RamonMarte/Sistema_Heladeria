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
    /// Lógica de interacción para CajasWindow.xaml
    /// </summary>
    public partial class CajasWindow : Window
    {
        private readonly Window _ventanaAnterior;
        private readonly CajasViewModel _viewModel;

        public CajasWindow(
            ApiClient apiClient,
            Window ventanaAnterior)
        {
            InitializeComponent();

            _ventanaAnterior = ventanaAnterior;

            _viewModel = new CajasViewModel(apiClient);

            DataContext = _viewModel;

            Loaded += CajasWindow_Loaded;
            Closed += CajasWindow_Closed;
        }

        private void CajasWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            _viewModel.CargarCommand.Execute(null);
        }

        private void Volver_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        private void CajasWindow_Closed(
            object? sender,
            EventArgs e)
        {
            _ventanaAnterior.WindowState = WindowState.Normal;
            _ventanaAnterior.Activate();
        }
    }
}
