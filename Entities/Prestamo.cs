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
        [MaxLength(50)]
        public string TipoPrestamo { get; set; } = "Personal"; // Personal, Hipotecario, Fiduciario, etc.

        [Required]
        [MaxLength(50)]
        public string MetodoDesembolso { get; set; } = "Efectivo"; // Efectivo, Transferencia

        [Required]
        [MaxLength(20)]
        public string TipoInteres { get; set; } = "Fijo"; // Fijo, Variable

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TasaMoraPorcentaje { get; set; } = 5.00m; // Porcentaje de interés moratorio por defecto

        [Required]
        public DateTime FechaOtorgado { get; set; } = DateTime.UtcNow;

        // Relación: Un préstamo puede tener muchos abonos/pagos
        public virtual ICollection<Pago> Pagos { get; set; } = new List<Pago>();

        // Relación: Un préstamo tiene su cronograma de cuotas
        public virtual ICollection<Cuota> Cuotas { get; set; } = new List<Cuota>();
    }
}
