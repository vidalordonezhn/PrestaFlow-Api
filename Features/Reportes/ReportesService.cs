using Microsoft.EntityFrameworkCore;
using PrestaFlow.API.Data;
using PrestaFlow.API.Features.Reportes.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrestaFlow.API.Features.Reportes
{
    public class ReportesService
    {
        private readonly PrestaFlowDbContext _context;

        public ReportesService(PrestaFlowDbContext context)
        {
            _context = context;
        }

        public async Task<ResumenCarteraDto> GetResumenCarteraAsync()
        {
            var prestamosActivos = await _context.Prestamos
                .Where(p => p.Status != "Pagado")
                .ToListAsync();

            decimal capitalColocado = 0;
            decimal interesPendiente = 0;
            int clientesMoraActiva = 0;

            var hoy = DateTime.UtcNow;

            foreach (var p in prestamosActivos)
            {
                // Calcular cuotas pagadas y saldo pendiente de capital/interés
                decimal ratioPendiente = 1 - ((decimal)p.CuotasPagadas / p.PlazoCuotas);
                
                capitalColocado += p.Capital * ratioPendiente;
                interesPendiente += (p.Capital * (p.InteresPorcentaje / 100)) * ratioPendiente;

                // Verificar si está en mora basándonos en el schedule
                var diasTranscurridos = (hoy - p.FechaOtorgado).Days;
                if (diasTranscurridos > 0)
                {
                    int cuotasEsperadas = p.Frecuencia switch
                    {
                        "Diario" => diasTranscurridos,
                        "Semanal" => diasTranscurridos / 7,
                        "Quincenal" => diasTranscurridos / 15,
                        "Mensual" => diasTranscurridos / 30,
                        _ => diasTranscurridos
                    };

                    if (cuotasEsperadas > p.PlazoCuotas) cuotasEsperadas = p.PlazoCuotas;

                    if (cuotasEsperadas > p.CuotasPagadas)
                    {
                        clientesMoraActiva++;
                    }
                }
            }

            return new ResumenCarteraDto
            {
                CapitalColocado = capitalColocado,
                InteresPendiente = interesPendiente,
                TotalProyectado = capitalColocado + interesPendiente,
                ClientesMoraActiva = clientesMoraActiva
            };
        }

        public async Task<IngresosReporteDto> GetIngresosReporteAsync(DateTime startDate, DateTime endDate)
        {
            // Traer todos los pagos en el rango de fecha
            var pagos = await _context.Pagos
                .Include(p => p.Prestamo)
                .Where(p => p.FechaPago >= startDate && p.FechaPago <= endDate)
                .ToListAsync();

            decimal total = 0;
            decimal capital = 0;
            decimal interes = 0;

            foreach (var p in pagos)
            {
                total += p.Monto;

                // Factor de capitalización: 1 / (1 + (Interes / 100))
                decimal capitalFactor = 1 / (1 + (p.Prestamo.InteresPorcentaje / 100));
                decimal capitalPortion = p.Monto * capitalFactor;
                decimal interesPortion = p.Monto - capitalPortion;

                capital += capitalPortion;
                interes += interesPortion;
            }

            return new IngresosReporteDto
            {
                Total = total,
                Capital = capital,
                Interes = interes
            };
        }

        public async Task<List<MoraDeudorDto>> GetDeudoresMoraAsync()
        {
            var hoy = DateTime.UtcNow;
            var moraList = new List<MoraDeudorDto>();

            var prestamosActivos = await _context.Prestamos
                .Include(p => p.Cliente)
                .Where(p => p.Status != "Pagado")
                .ToListAsync();

            foreach (var p in prestamosActivos)
            {
                var diasTranscurridos = (hoy - p.FechaOtorgado).Days;
                if (diasTranscurridos <= 0) continue;

                int cuotasEsperadas = p.Frecuencia switch
                {
                    "Diario" => diasTranscurridos,
                    "Semanal" => diasTranscurridos / 7,
                    "Quincenal" => diasTranscurridos / 15,
                    "Mensual" => diasTranscurridos / 30,
                    _ => diasTranscurridos
                };

                if (cuotasEsperadas > p.PlazoCuotas) cuotasEsperadas = p.PlazoCuotas;

                if (cuotasEsperadas > p.CuotasPagadas)
                {
                    int cuotasVencidas = cuotasEsperadas - p.CuotasPagadas;
                    decimal montoAtrasado = cuotasVencidas * p.CuotaMonto;
                    
                    int diasDeRetraso = p.Frecuencia switch
                    {
                        "Diario" => cuotasVencidas,
                        "Semanal" => cuotasVencidas * 7,
                        "Quincenal" => cuotasVencidas * 15,
                        "Mensual" => cuotasVencidas * 30,
                        _ => cuotasVencidas
                    };

                    string nivelRiesgo = "Bajo";
                    if (diasDeRetraso > 15) nivelRiesgo = "Alto";
                    else if (diasDeRetraso > 7) nivelRiesgo = "Medio";

                    moraList.Add(new MoraDeudorDto
                    {
                        ClienteNombre = p.Cliente.Nombre,
                        ClienteIdentidad = p.Cliente.Identidad,
                        PrestamoCodigo = $"PF-{p.Id:D4}",
                        CuotasVencidas = cuotasVencidas,
                        DiasRetraso = diasDeRetraso,
                        MontoAtrasado = montoAtrasado,
                        NivelRiesgo = nivelRiesgo
                    });
                }
            }

            return moraList.OrderByDescending(m => m.DiasRetraso).ToList();
        }
    }
}
