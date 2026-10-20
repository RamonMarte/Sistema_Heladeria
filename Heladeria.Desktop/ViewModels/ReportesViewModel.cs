using Heladeria.Contracts.Reportes;
using Heladeria.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;

namespace Heladeria.Desktop.ViewModels
{
    public sealed class ReportesViewModel : INotifyPropertyChanged
    {
        private readonly ApiClient _apiClient;

        private bool _ocupado;
        private string _mensaje = string.Empty;
        private string _periodoSeleccionado = "hoy";
        private string _usuarioIdFiltro = string.Empty;
        private string _productoPresentacionIdFiltro = string.Empty;
        private string _cajaIdFiltro = string.Empty;
        private DateTime? _fechaDesde;
        private DateTime? _fechaHasta;

        private ReporteVentasDashboardResponse _resumen = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public ReportesViewModel(ApiClient apiClient)
        {
            _apiClient = apiClient;

            CargarCommand = new ComandoAsync(CargarAsync, () => !Ocupado);

            PeriodosDisponibles = new ObservableCollection<string>
        {
            "hoy",
            "semana",
            "mes",
            "trimestre",
            "semestre",
            "anio"
        };
        }

        public ICommand CargarCommand { get; }

        public ObservableCollection<string> PeriodosDisponibles { get; }

        public ObservableCollection<PagoMetodoReporteDashboardResponse>
            PagosPorMetodo
        { get; } = new();

        public ObservableCollection<ProductoMasVendidoDashboardResponse>
            ProductosMasVendidos
        { get; } = new();

        public ObservableCollection<VentaPorDiaDashboardResponse>
            VentasPorDia
        { get; } = new();

        public ObservableCollection<VentaEmpleadoDashboardResponse>
            VentasPorEmpleado
        { get; } = new();

        public bool Ocupado
        {
            get => _ocupado;
            private set
            {
                if (Cambiar(ref _ocupado, value))
                    ((ComandoAsync)CargarCommand).ActualizarEstado();
            }
        }

        public string Mensaje
        {
            get => _mensaje;
            private set => Cambiar(ref _mensaje, value);
        }

        public string PeriodoSeleccionado
        {
            get => _periodoSeleccionado;
            set => Cambiar(ref _periodoSeleccionado, value);
        }

        public DateTime? FechaDesde
        {
            get => _fechaDesde;
            set => Cambiar(ref _fechaDesde, value);
        }

        public DateTime? FechaHasta
        {
            get => _fechaHasta;
            set => Cambiar(ref _fechaHasta, value);
        }

        public string UsuarioIdFiltro
        {
            get => _usuarioIdFiltro;
            set => Cambiar(ref _usuarioIdFiltro, value);
        }

        public string ProductoPresentacionIdFiltro
        {
            get => _productoPresentacionIdFiltro;
            set => Cambiar(ref _productoPresentacionIdFiltro, value);
        }

        public string CajaIdFiltro
        {
            get => _cajaIdFiltro;
            set => Cambiar(ref _cajaIdFiltro, value);
        }

        public ReporteVentasDashboardResponse Resumen
        {
            get => _resumen;
            private set
            {
                if (Cambiar(ref _resumen, value))
                {
                    NotificarResumen();
                }
            }
        }

        public int CantidadTransacciones =>
            Resumen.CantidadTransacciones;

        public decimal ImporteTotalTickets =>
            Resumen.ImporteTotalTickets;

        public decimal DescuentosTickets =>
            Resumen.DescuentosTickets;

        public decimal UnidadesVendidas =>
            Resumen.UnidadesVendidas;

        public decimal ImporteProductos =>
            Resumen.ImporteProductos;

        public decimal CostoEstimadoProductos =>
            Resumen.CostoEstimadoProductos;

        public decimal GananciaBrutaEstimada =>
            Resumen.GananciaBrutaEstimada;

        public decimal MaximoImportePorDia =>
            VentasPorDia.Count == 0
                ? 1m
                : Math.Max(
                    VentasPorDia.Max(x => x.ImporteTotal),
                    1m);

        public decimal MaximoImporteProducto =>
            ProductosMasVendidos.Count == 0
                ? 1m
                : Math.Max(
                    ProductosMasVendidos.Max(x => x.Importe),
                    1m);

        private void NotificarResumen()
        {
            OnPropertyChanged(nameof(CantidadTransacciones));
            OnPropertyChanged(nameof(ImporteTotalTickets));
            OnPropertyChanged(nameof(DescuentosTickets));
            OnPropertyChanged(nameof(UnidadesVendidas));
            OnPropertyChanged(nameof(ImporteProductos));
            OnPropertyChanged(nameof(CostoEstimadoProductos));
            OnPropertyChanged(nameof(GananciaBrutaEstimada));
        }

        private async Task CargarAsync()
        {
            try
            {
                Ocupado = true;
                Mensaje = "Consultando reportes...";

                string filtros = ConstruirFiltros();

                var resumen = await ObtenerAsync
                    <ReporteVentasDashboardResponse>(
                        $"api/reportes/ventas?{filtros}");

                var empleados = await ObtenerAsync
                    <List<VentaEmpleadoDashboardResponse>>(
                        $"api/reportes/ventas-por-empleado?{filtros}");

                Resumen = resumen;

                Reemplazar(PagosPorMetodo, resumen.PagosPorMetodo);
                Reemplazar(ProductosMasVendidos, resumen.ProductosMasVendidos);
                Reemplazar(VentasPorDia, resumen.VentasPorDia);
                Reemplazar(VentasPorEmpleado, empleados);
                OnPropertyChanged(nameof(MaximoImportePorDia));
                OnPropertyChanged(nameof(MaximoImporteProducto));

                Mensaje = "Reportes cargados correctamente.";
            }
            catch (Exception ex)
            {
                Mensaje = $"No se pudieron cargar los reportes: {ex.Message}";
            }
            finally
            {
                Ocupado = false;
            }
        }

        private string ConstruirFiltros()
        {
            var parametros = new List<string>();

            parametros.Add(
                $"periodo={Uri.EscapeDataString(PeriodoSeleccionado)}");

            AgregarId(parametros, "usuarioId", UsuarioIdFiltro);
            AgregarId(
                parametros,
                "productoPresentacionId",
                ProductoPresentacionIdFiltro);

            AgregarId(parametros, "cajaId", CajaIdFiltro);

            if (FechaDesde.HasValue && FechaHasta.HasValue &&
                FechaDesde.Value.Date > FechaHasta.Value.Date)
            {
                throw new InvalidOperationException(
                    "La fecha inicial no puede ser posterior a la fecha final.");
            }

            if (FechaDesde.HasValue)
            {
                var desde = new DateTimeOffset(FechaDesde.Value.Date);

                parametros.Add(
                    $"desde={Uri.EscapeDataString(desde.ToString("O", CultureInfo.InvariantCulture))}");
            }

            if (FechaHasta.HasValue)
            {
                var hasta = new DateTimeOffset(
                    FechaHasta.Value.Date.AddDays(1).AddTicks(-1));

                parametros.Add(
                    $"hasta={Uri.EscapeDataString(hasta.ToString("O", CultureInfo.InvariantCulture))}");
            }

            return string.Join("&", parametros);
        }

        private static void AgregarId(
            List<string> parametros,
            string nombre,
            string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return;

            if (!int.TryParse(valor, out int id) || id <= 0)
            {
                throw new InvalidOperationException(
                    $"El filtro {nombre} debe ser un número entero positivo.");
            }

            parametros.Add($"{nombre}={id}");
        }

        private async Task<T> ObtenerAsync<T>(string ruta)
        {
            using HttpResponseMessage respuesta =
                await _apiClient.Http.GetAsync(ruta);

            if (!respuesta.IsSuccessStatusCode)
            {
                string detalle = await respuesta.Content.ReadAsStringAsync();

                throw new HttpRequestException(
                    $"La API respondió {(int)respuesta.StatusCode} " +
                    $"{respuesta.ReasonPhrase}. {detalle}");
            }

            return await respuesta.Content.ReadFromJsonAsync<T>()
                ?? throw new InvalidOperationException(
                    "La API devolvió una respuesta vacía.");
        }

        private static void Reemplazar<T>(
            ObservableCollection<T> destino,
            IEnumerable<T> origen)
        {
            destino.Clear();

            foreach (var elemento in origen)
                destino.Add(elemento);
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

        private sealed class ComandoAsync : ICommand
        {
            private readonly Func<Task> _ejecutar;
            private readonly Func<bool> _puedeEjecutar;
            private bool _ejecutando;

            public ComandoAsync(
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

                try
                {
                    _ejecutando = true;
                    ActualizarEstado();

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

        public async Task<bool> ActualizarParaExportarAsync()
        {
            if (Ocupado)
                return false;

            await CargarAsync();

            return Mensaje == "Reportes cargados correctamente.";
        }
    }
}
