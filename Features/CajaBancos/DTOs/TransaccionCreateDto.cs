using System.ComponentModel.DataAnnotations;

namespace PrestaFlow.API.Features.CajaBancos.DTOs
{
    public class TransaccionCreateDto
    {
        [Required(ErrorMessage = "El identificador de la cuenta es requerido.")]
        public int CuentaId { get; set; }

        [Required(ErrorMessage = "El tipo de transacción (Ingreso / Egreso) es requerido.")]
        [RegularExpression("^(Ingreso|Egreso)$", ErrorMessage = "El tipo debe ser 'Ingreso' o 'Egreso'.")]
        public string Tipo { get; set; } = null!;

        [Required(ErrorMessage = "El monto es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a cero.")]
        public decimal Monto { get; set; }

        [Required(ErrorMessage = "El concepto de la transacción es requerido.")]
        [MaxLength(250, ErrorMessage = "El concepto no puede superar los 250 caracteres.")]
        public string Concepto { get; set; } = null!;
    }
}
