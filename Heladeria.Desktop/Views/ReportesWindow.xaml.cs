using Heladeria.Desktop.Services;
using Heladeria.Desktop.ViewModels;
using Microsoft.Win32;
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
    /// Lógica de interacción para ReportesWindow.xaml
    /// </summary>
    public partial class ReportesWindow : Window
    {
        private readonly Window _ventanaAnterior;
        private readonly ReportesViewModel _viewModel;

        public ReportesWindow(
            ApiClient apiClient,
            Window ventanaAnterior)
        {
            InitializeComponent();

            _ventanaAnterior = ventanaAnterior;

            _viewModel = new ReportesViewModel(apiClient);

            DataContext = _viewModel;

            Loaded += ReportesWindow_Loaded;
            Closed += ReportesWindow_Closed;
        }

        private void ReportesWindow_Loaded(
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

        private void ReportesWindow_Closed(
            object? sender,
            EventArgs e)
        {
            _ventanaAnterior.WindowState = WindowState.Normal;
            _ventanaAnterior.Activate();
        }

        private async void ExportarPdf_Click(
            object sender,
            RoutedEventArgs e)
        {
            var dialogo = new SaveFileDialog
            {
                Title = "Guardar reporte de ventas",
                Filter = "Archivo PDF (*.pdf)|*.pdf",
                DefaultExt = ".pdf",
                AddExtension = true,
                FileName = $"ReporteVentas_{DateTime.Now:yyyyMMdd_HHmm}.pdf"
            };

            if (dialogo.ShowDialog(this) != true)
                return;

            try
            {
                bool actualizado =
                    await _viewModel.ActualizarParaExportarAsync();

                if (!actualizado)
                {
                    MessageBox.Show(
                        _viewModel.Mensaje,
                        "No se pudo actualizar el reporte",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                ReportesPdfService.Generar(
                    _viewModel.Resumen,
                    _viewModel.VentasPorEmpleado,
                    dialogo.FileName);

                MessageBox.Show(
                    $"El reporte PDF se generó correctamente.\n\n" +
                    $"Ubicación:\n{dialogo.FileName}",
                    "Exportación completada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudo generar el PDF.\n\n{ex.Message}",
                    "Error de exportación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
