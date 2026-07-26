using System.ComponentModel.DataAnnotations;

namespace PrestaFlow.API.Features.Pagos.DTOs
{
    public class PagoCreateDto
    {
        [Required(ErrorMessage = "El identificador del préstamo es requerido.")]
        public int PrestamoId { get; set; }

        [Required(ErrorMessage = "El monto del abono es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto a pagar debe ser mayor a cero.")]
        public decimal Monto { get; set; }

        [Required(ErrorMessage = "El método de pago es requerido.")]
        [RegularExpression("^(Efectivo|Transferencia)$", ErrorMessage = "El método de pago debe ser 'Efectivo' o 'Transferencia'.")]
        public string MetodoPago { get; set; } = "Efectivo";

        [MaxLength(100, ErrorMessage = "La referencia no puede exceder los 100 caracteres.")]
        public string? Referencia { get; set; }
    }
}
