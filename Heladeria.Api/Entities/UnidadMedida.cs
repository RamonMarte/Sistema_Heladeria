namespace Heladeria.Api.Entities
{
    public class UnidadMedida
    {
        public int UnidadMedidaId { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Abreviatura { get; set; } = string.Empty;

        public bool Activa { get; set; }
    }
}
