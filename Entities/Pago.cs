using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrestaFlow.API.Entities
{
    public class Pago : AuditableEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PrestamoId { get; set; }

        [ForeignKey("PrestamoId")]
        public virtual Prestamo Prestamo { get; set; } = null!;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        [Required]
        public DateTime FechaPago { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(20)]
        public string MetodoPago { get; set; } = "Efectivo"; // Efectivo, Transferencia

        [MaxLength(100)]
        public string? Referencia { get; set; }
    }
}
