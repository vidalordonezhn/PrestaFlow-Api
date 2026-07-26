using System.ComponentModel.DataAnnotations;

namespace PrestaFlow.API.Features.CajaBancos.DTOs
{
    public class TransferenciaCreateDto
    {
        [Required(ErrorMessage = "La cuenta de origen es requerida.")]
        public int CuentaOrigenId { get; set; }

        [Required(ErrorMessage = "La cuenta de destino es requerida.")]
        public int CuentaDestinoId { get; set; }

        [Required(ErrorMessage = "El monto es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a cero.")]
        public decimal Monto { get; set; }

        [Required(ErrorMessage = "El concepto de la transferencia es requerido.")]
        [MaxLength(250, ErrorMessage = "El concepto no puede superar los 250 caracteres.")]
        public string Concepto { get; set; } = null!;
    }
}
