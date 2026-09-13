using System;
using System.Collections.Generic;

namespace PrestaFlow.API.Features.CajaBancos.DTOs
{
    public class MovimientoArqueoDto
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string CuentaNombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty; // Ingreso / Egreso
        public decimal Monto { get; set; }
        public string Concepto { get; set; } = string.Empty;
    }

    public class ArqueoDiarioDto
    {
        public DateTime FechaConsulta { get; set; }
        public decimal TotalEfectivoIngresos { get; set; }
        public decimal TotalTransferenciasIngresos { get; set; }
        public decimal TotalDesembolsosEgresos { get; set; }
        public decimal TotalOtrosEgresos { get; set; }
        public decimal BalanceNetoDia { get; set; }
        public decimal SaldoTotalCajas { get; set; }
        public decimal SaldoTotalBancos { get; set; }
        public int TotalOperaciones { get; set; }
        public List<MovimientoArqueoDto> Movimientos { get; set; } = new();
    }
}
