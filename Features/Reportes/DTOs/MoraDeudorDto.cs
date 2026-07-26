namespace PrestaFlow.API.Features.Reportes.DTOs
{
    public class MoraDeudorDto
    {
        public string ClienteNombre { get; set; } = null!;
        public string ClienteIdentidad { get; set; } = null!;
        public string PrestamoCodigo { get; set; } = null!;
        public int DiasRetraso { get; set; }
        public int CuotasVencidas { get; set; }
        public decimal MontoAtrasado { get; set; }
        public string NivelRiesgo { get; set; } = "Bajo";
    }
}
