using System;

namespace PrestaFlow.API.Features.Pagos.DTOs
{
    public class PagoResponseDto
    {
        public int Id { get; set; }
        public int PrestamoId { get; set; }
        public string PrestamoCodigo { get; set; } = null!;
        public string ClienteNombre { get; set; } = null!;
        public string ClienteIdentidad { get; set; } = null!;
        public string ClientePhone { get; set; } = null!;
        public decimal Monto { get; set; }
        public decimal MontoPrincipal { get; set; }
        public decimal MontoInteres { get; set; }
        public decimal MontoMora { get; set; }
        public DateTime FechaPago { get; set; }
        public string MetodoPago { get; set; } = null!;
        public string? Referencia { get; set; }
        public string CreadoPor { get; set; } = null!;
        public bool EsAnulado { get; set; }
        public string? MotivoAnulacion { get; set; }
        public DateTime? FechaAnulacion { get; set; }
    }
}
