using System;
using System.Collections.Generic;
using System.Text;

namespace Heladeria.Desktop.ViewModels
{
    public sealed class RecetaLineaViewModel : ViewModelBase
    {
        private string _cantidadTexto = "1";

        public int ArticuloInventarioId { get; set; }

        public string NombreArticulo { get; set; } = string.Empty;

        public string TipoArticulo { get; set; } = string.Empty;

        public string UnidadMedida { get; set; } = string.Empty;

        public string Abreviatura { get; set; } = string.Empty;

        public string CantidadTexto
        {
            get => _cantidadTexto;
            set => SetProperty(ref _cantidadTexto, value);
        }
    }
}
