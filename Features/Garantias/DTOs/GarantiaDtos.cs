using System;
using System.ComponentModel.DataAnnotations;

namespace PrestaFlow.API.Features.Garantias.DTOs
{
    public class GarantiaResponseDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public int ClienteId { get; set; }
        public string ClienteNombre { get; set; } = string.Empty;
        public string ClienteIdentidad { get; set; } = string.Empty;
        public string ClientePhone { get; set; } = string.Empty;
        public int? PrestamoId { get; set; }
        public string? PrestamoCodigo { get; set; }
        public decimal? PrestamoSaldoRestante { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string? NumeroSerie { get; set; }
        public decimal ValorEstimado { get; set; }
        public string EstadoCustodia { get; set; } = string.Empty;
        public string UbicacionFisica { get; set; } = string.Empty;
        public string? FotoUrl { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaIngreso { get; set; }
        public DateTime? FechaDevolucion { get; set; }
        public DateTime FechaCreacion { get; set; }
        public string CreadoPor { get; set; } = string.Empty;
    }

    public class CrearGarantiaDto
    {
        [Required(ErrorMessage = "El cliente es obligatorio.")]
        public int ClienteId { get; set; }

        public int? PrestamoId { get; set; }

        [Required(ErrorMessage = "El tipo de bien es obligatorio.")]
        [MaxLength(50)]
        public string Tipo { get; set; } = "Vehículo";

        [Required(ErrorMessage = "La descripción de la garantía es obligatoria.")]
        [MaxLength(250)]
        public string Descripcion { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? NumeroSerie { get; set; }

        [Required(ErrorMessage = "El valor estimado de tasación es obligatorio.")]
        [Range(0.01, 10000000, ErrorMessage = "El valor de tasación debe ser mayor a 0.")]
        public decimal ValorEstimado { get; set; }

        [Required(ErrorMessage = "La ubicación física en bodega o sucursal es obligatoria.")]
        [MaxLength(150)]
        public string UbicacionFisica { get; set; } = "Bodega Central";

        public string? FotoUrl { get; set; }

        [MaxLength(500)]
        public string? Observaciones { get; set; }
    }

    public class ActualizarGarantiaDto
    {
        public int? PrestamoId { get; set; }

        [Required(ErrorMessage = "El tipo de bien es obligatorio.")]
        [MaxLength(50)]
        public string Tipo { get; set; } = string.Empty;

        [Required(ErrorMessage = "La descripción de la garantía es obligatoria.")]
        [MaxLength(250)]
        public string Descripcion { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? NumeroSerie { get; set; }

        [Required(ErrorMessage = "El valor estimado de tasación es obligatorio.")]
        [Range(0.01, 10000000, ErrorMessage = "El valor de tasación debe ser mayor a 0.")]
        public decimal ValorEstimado { get; set; }

        [Required(ErrorMessage = "La ubicación física en bodega es obligatoria.")]
        [MaxLength(150)]
        public string UbicacionFisica { get; set; } = string.Empty;

        public string? FotoUrl { get; set; }

        [MaxLength(500)]
        public string? Observaciones { get; set; }
    }

    public class CambiarEstadoGarantiaDto
    {
        [Required(ErrorMessage = "El nuevo estado de custodia es obligatorio.")]
        public string EstadoCustodia { get; set; } = "Devuelta"; // En Custodia, Devuelta, En Remate, Liquidada

        [MaxLength(500)]
        public string? Observaciones { get; set; }
    }
}
