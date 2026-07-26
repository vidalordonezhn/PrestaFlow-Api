using System.ComponentModel.DataAnnotations;

namespace PrestaFlow.API.Features.Prestamos.DTOs
{
    public class PrestamoCreateDto
    {
        [Required(ErrorMessage = "El identificador del cliente es requerido.")]
        public int ClienteId { get; set; }

        [Required(ErrorMessage = "El monto de capital solicitado es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El capital debe ser mayor a cero.")]
        public decimal Capital { get; set; }

        [Required(ErrorMessage = "La tasa de interés es requerida.")]
        [Range(0, 100, ErrorMessage = "La tasa de interés debe estar entre 0% y 100%.")]
        public decimal InteresPorcentaje { get; set; }

        [Required(ErrorMessage = "El plazo (número de cuotas) es requerido.")]
        [Range(1, 1000, ErrorMessage = "El plazo debe ser de al menos 1 cuota.")]
        public int PlazoCuotas { get; set; }

        [Required(ErrorMessage = "La frecuencia del préstamo es requerida.")]
        [RegularExpression("^(Diario|Semanal|Mensual)$", ErrorMessage = "La frecuencia debe ser 'Diario', 'Semanal' o 'Mensual'.")]
        public string Frecuencia { get; set; } = "Diario";

        [Required(ErrorMessage = "La cuenta de desembolso es requerida.")]
        public int CuentaDesembolsoId { get; set; }
    }
}
