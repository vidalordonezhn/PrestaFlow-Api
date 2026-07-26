using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrestaFlow.API.Entities
{
    public class CuentaFinanciera : AuditableEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        public string Tipo { get; set; } = "Caja"; // Caja, Banco

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Saldo { get; set; } = 0;

        // Relación: Una cuenta puede tener muchas transacciones
        public virtual ICollection<TransaccionFinanciera> Transacciones { get; set; } = new List<TransaccionFinanciera>();
    }
}
