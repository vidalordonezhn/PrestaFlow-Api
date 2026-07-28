namespace PrestaFlow.API.Features.Reportes.DTOs
{
    public class ResumenCarteraDto
    {
        public decimal CapitalColocado { get; set; }
        public decimal InteresPendiente { get; set; }
        public decimal TotalProyectado { get; set; }
        public int ClientesMoraActiva { get; set; }
        public decimal CapitalHistoricoPrestado { get; set; }
        public decimal CapitalActual { get; set; }
    }
}
