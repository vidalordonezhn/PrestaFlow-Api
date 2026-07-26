using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrestaFlow.API.Entities
{
    public class TransaccionFinanciera : AuditableEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CuentaId { get; set; }

        [ForeignKey("CuentaId")]
        public virtual CuentaFinanciera Cuenta { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        public string Tipo { get; set; } = "Ingreso"; // Ingreso, Egreso, Transferencia

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        [Required]
        [MaxLength(250)]
        public string Concepto { get; set; } = null!;

        [Required]
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }
}
