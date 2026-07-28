using System;

namespace PrestaFlow.API.Features.Prestamos.DTOs
{
    public class PrestamoResponseDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = null!; // E.g. "PF-1001"
        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; } = null!;
        public string ClientePhone { get; set; } = null!;
        public decimal Capital { get; set; }
        public decimal InteresPorcentaje { get; set; }
        public int PlazoCuotas { get; set; }
        public decimal CuotaMonto { get; set; }
        public int CuotasPagadas { get; set; }
        public string Status { get; set; } = null!; // Activo, Pagado, Mora
        public string Frecuencia { get; set; } = null!; // Diario, Semanal, Mensual
        public string TipoPrestamo { get; set; } = null!;
        public string MetodoDesembolso { get; set; } = null!;
        public string TipoInteres { get; set; } = null!;
        public decimal TasaMoraPorcentaje { get; set; }
        public DateTime FechaOtorgado { get; set; }
        public System.Collections.Generic.List<CuotaResponseDto> Cuotas { get; set; } = new();
    }
}
