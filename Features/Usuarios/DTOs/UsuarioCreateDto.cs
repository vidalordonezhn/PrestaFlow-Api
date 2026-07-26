using System.ComponentModel.DataAnnotations;

namespace PrestaFlow.API.Features.Usuarios.DTOs
{
    public class UsuarioCreateDto
    {
        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = null!;

        [Required]
        [MinLength(4)]
        public string Password { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        public string Rol { get; set; } = "Cobrador"; // Admin, Cobrador
    }
}
