using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PrestaFlow.API.Entities
{
    public class Cliente : AuditableEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Identidad { get; set; } = null!; // Identidad o Cédula

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        public string Phone { get; set; } = null!;

        [Required]
        [MaxLength(250)]
        public string Address { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string Zone { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string RefName { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        public string RefPhone { get; set; } = null!;

        // Relación: Un cliente puede tener muchos préstamos
        public virtual ICollection<Prestamo> Prestamos { get; set; } = new List<Prestamo>();
    }
}
