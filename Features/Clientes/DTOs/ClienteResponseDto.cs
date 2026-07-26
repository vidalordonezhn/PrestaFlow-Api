using System;
using System.Collections.Generic;

namespace PrestaFlow.API.Features.Clientes.DTOs
{
    public class ClienteResponseDto
    {
        public int Id { get; set; }
        public string Identidad { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string Zone { get; set; } = null!;
        public string RefName { get; set; } = null!;
        public string RefPhone { get; set; } = null!;
        
        // Indicadores Reactivos
        public int LoansCount { get; set; }
        public decimal Balance { get; set; }
        public string Status { get; set; } = "Sin Crédito"; // Al Día, En Mora, Sin Crédito
        public string Score { get; set; } = "Nuevo"; // Excelente, Regular, Mora, Nuevo

        // Historial crediticio detallado para la ficha técnica
        public List<ClienteLoanHistoryDto> PrestamosHistory { get; set; } = new();
    }

    public class ClienteLoanHistoryDto
    {
        public string LoanId { get; set; } = null!;
        public decimal Amount { get; set; }
        public decimal Interest { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; } = null!; // Activo, Pagado, Mora
        public string Cuotas { get; set; } = null!; // Ej. "5/20"
    }
}
