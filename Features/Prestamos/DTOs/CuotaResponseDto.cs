using System;

namespace PrestaFlow.API.Features.Prestamos.DTOs
{
    public class CuotaResponseDto
    {
        public int Id { get; set; }
        public int NumeroCuota { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public decimal MontoPrincipal { get; set; }
        public decimal MontoInteres { get; set; }
        public decimal MontoMoratorio { get; set; }
        public decimal MontoPagadoPrincipal { get; set; }
        public decimal MontoPagadoInteres { get; set; }
        public decimal MontoPagadoMora { get; set; }
        public string Estado { get; set; } = null!;
        public DateTime? FechaUltimoCalculoMora { get; set; }
    }
}
