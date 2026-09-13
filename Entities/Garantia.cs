using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrestaFlow.API.Entities
{
    public class Garantia : AuditableEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Codigo { get; set; } = string.Empty; // ej. GAR-0001

        [Required]
        public int ClienteId { get; set; }

        [ForeignKey("ClienteId")]
        public virtual Cliente Cliente { get; set; } = null!;

        public int? PrestamoId { get; set; }

        [ForeignKey("PrestamoId")]
        public virtual Prestamo? Prestamo { get; set; }

        [Required]
        [MaxLength(50)]
        public string Tipo { get; set; } = "Vehículo"; // Vehículo, Inmueble, Electrodoméstico, Joya / Oro, Maquinaria, Otro

        [Required]
        [MaxLength(250)]
        public string Descripcion { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? NumeroSerie { get; set; } // VIN, Chasis, Matrícula, IMEI, N° Serie

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ValorEstimado { get; set; } // Valor de tasación en Lempiras

        [Required]
        [MaxLength(50)]
        public string EstadoCustodia { get; set; } = "En Custodia"; // En Custodia, Devuelta, En Remate, Liquidada

        [Required]
        [MaxLength(150)]
        public string UbicacionFisica { get; set; } = "Bodega Central"; // Bodega, Estante, Bóveda, Parqueo

        public string? FotoUrl { get; set; } // URL o Data URI (Base64) de la fotografía

        [MaxLength(500)]
        public string? Observaciones { get; set; }

        [Required]
        public DateTime FechaIngreso { get; set; } = DateTime.UtcNow;

        public DateTime? FechaDevolucion { get; set; }
    }
}
