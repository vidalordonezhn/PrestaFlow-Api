using System.ComponentModel.DataAnnotations;

namespace PrestaFlow.API.Features.Clientes.DTOs
{
    public class ClienteCreateDto
    {
        [Required(ErrorMessage = "La identidad/cédula del cliente es requerida.")]
        [MaxLength(20, ErrorMessage = "La identidad no puede exceder los 20 caracteres.")]
        public string Identidad { get; set; } = null!;

        [Required(ErrorMessage = "El nombre completo del cliente es requerido.")]
        [MaxLength(100, ErrorMessage = "El nombre no puede exceder los 100 caracteres.")]
        public string Nombre { get; set; } = null!;

        [Required(ErrorMessage = "El teléfono celular es requerido.")]
        [MaxLength(20, ErrorMessage = "El teléfono no puede exceder los 20 caracteres.")]
        public string Phone { get; set; } = null!;

        [Required(ErrorMessage = "La dirección de domicilio es requerida.")]
        [MaxLength(250, ErrorMessage = "La dirección no puede exceder los 250 caracteres.")]
        public string Address { get; set; } = null!;

        [Required(ErrorMessage = "La zona de cobro es requerida.")]
        [MaxLength(50, ErrorMessage = "La zona no puede exceder los 50 caracteres.")]
        public string Zone { get; set; } = null!;

        [Required(ErrorMessage = "El nombre del aval o referencia familiar es requerido.")]
        [MaxLength(100, ErrorMessage = "El nombre de referencia no puede exceder los 100 caracteres.")]
        public string RefName { get; set; } = null!;

        [Required(ErrorMessage = "El teléfono del aval o referencia es requerido.")]
        [MaxLength(20, ErrorMessage = "El teléfono de referencia no puede exceder los 20 caracteres.")]
        public string RefPhone { get; set; } = null!;
    }
}
