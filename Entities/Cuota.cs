using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrestaFlow.API.Entities
{
    public class Cuota : AuditableEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PrestamoId { get; set; }

        [ForeignKey("PrestamoId")]
        public virtual Prestamo Prestamo { get; set; } = null!;

        [Required]
        public int NumeroCuota { get; set; }

        [Required]
        public DateTime FechaVencimiento { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoPrincipal { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoInteres { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoMoratorio { get; set; } = 0.00m;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoPagadoPrincipal { get; set; } = 0.00m;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoPagadoInteres { get; set; } = 0.00m;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoPagadoMora { get; set; } = 0.00m;

        [Required]
        [MaxLength(20)]
        public string Estado { get; set; } = "Pendiente"; // Pendiente, Parcial, Pagado, Vencido

        public DateTime? FechaUltimoCalculoMora { get; set; }
    }
}
