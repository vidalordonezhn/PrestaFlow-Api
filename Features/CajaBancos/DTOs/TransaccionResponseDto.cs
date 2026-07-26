using System;

namespace PrestaFlow.API.Features.CajaBancos.DTOs
{
    public class TransaccionResponseDto
    {
        public int Id { get; set; }
        public int CuentaId { get; set; }
        public string CuentaNombre { get; set; } = null!;
        public string Tipo { get; set; } = null!; // Ingreso, Egreso, Transferencia
        public decimal Monto { get; set; }
        public string Concepto { get; set; } = null!;
        public DateTime Fecha { get; set; }
        public string CreadoPor { get; set; } = null!;
    }
}
