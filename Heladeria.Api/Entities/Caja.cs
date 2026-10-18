namespace Heladeria.Api.Entities
{
    public class Caja
    {
        public int CajaId { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public string Nombre { get; set; } = string.Empty;

        public bool Activa { get; set; } = true;
    }
}
