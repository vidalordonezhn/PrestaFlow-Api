using System.ComponentModel.DataAnnotations;

namespace PrestaFlow.API.Features.Auth.DTOs
{
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "El nombre de usuario es requerido.")]
        public string Username { get; set; } = null!;

        [Required(ErrorMessage = "La contraseña es requerida.")]
        public string Password { get; set; } = null!;
    }
}
