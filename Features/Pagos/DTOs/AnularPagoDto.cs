using System.ComponentModel.DataAnnotations;

namespace PrestaFlow.API.Features.Pagos.DTOs
{
    public class AnularPagoDto
    {
        [Required(ErrorMessage = "El motivo de anulación es obligatorio.")]
        [MinLength(5, ErrorMessage = "El motivo debe tener al menos 5 caracteres.")]
        [MaxLength(250, ErrorMessage = "El motivo no puede exceder los 250 caracteres.")]
        public string Motivo { get; set; } = string.Empty;
    }
}
