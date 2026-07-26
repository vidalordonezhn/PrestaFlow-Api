using System.ComponentModel.DataAnnotations;

namespace PrestaFlow.API.Entities
{
    public class Usuario : AuditableEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = null!;

        [Required]
        public string PasswordHash { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        public string Rol { get; set; } = "Cobrador"; // Admin, Cobrador

        public bool Activo { get; set; } = true;
    }
}
