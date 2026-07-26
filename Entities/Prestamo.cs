using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrestaFlow.API.Entities
{
    public class Prestamo : AuditableEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClienteId { get; set; }

        [ForeignKey("ClienteId")]
        public virtual Cliente Cliente { get; set; } = null!;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Capital { get; set; }

        [Required]
        [Column(TypeName = "decimal(5,2)")]
        public decimal InteresPorcentaje { get; set; }

        [Required]
        public int PlazoCuotas { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CuotaMonto { get; set; }

        public int CuotasPagadas { get; set; } = 0;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Activo"; // Activo, Pagado, Mora

        [Required]
        [MaxLength(20)]
        public string Frecuencia { get; set; } = "Diario"; // Diario, Semanal, Mensual

        [Required]
        public DateTime FechaOtorgado { get; set; } = DateTime.UtcNow;

        // Relación: Un préstamo puede tener muchos abonos/pagos
        public virtual ICollection<Pago> Pagos { get; set; } = new List<Pago>();
    }
}
