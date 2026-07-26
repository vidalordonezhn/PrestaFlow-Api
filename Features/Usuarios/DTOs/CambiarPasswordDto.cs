using System.ComponentModel.DataAnnotations;

namespace PrestaFlow.API.Features.Usuarios.DTOs
{
    public class CambiarPasswordDto
    {
        [Required]
        public string Username { get; set; } = null!;

        [Required]
        [MinLength(4)]
        public string NewPassword { get; set; } = null!;
    }
}
