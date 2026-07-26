using System.ComponentModel.DataAnnotations;

namespace PrestaFlow.API.Features.CajaBancos.DTOs
{
    public class CuentaCreateDto
    {
        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        public string Tipo { get; set; } = null!; // Caja, Banco

        [Range(0, double.MaxValue)]
        public decimal Saldo { get; set; }
    }
}
