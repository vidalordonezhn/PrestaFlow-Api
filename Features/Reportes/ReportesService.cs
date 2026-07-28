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
            var prestamos = await _context.Prestamos
                .Include(p => p.Cuotas)
                .ToListAsync();

            // Ejecutar recálculo dinámico en memoria para que los datos estén al día
            foreach (var p in prestamos)
            {
                PrestaFlow.API.Features.Pagos.PagosService.ActualizarMoraYRecalculos(p);
            }

            decimal capitalHistorico = prestamos.Sum(p => p.Capital);
            decimal capitalActual = prestamos.Sum(p => p.Capital - p.Cuotas.Sum(c => c.MontoPagadoPrincipal));
            decimal capitalColocado = prestamos.Where(p => p.Status != "Pagado").Sum(p => p.Cuotas.Sum(c => c.MontoPrincipal - c.MontoPagadoPrincipal));
            decimal interesPendiente = prestamos.Where(p => p.Status != "Pagado").Sum(p => p.Cuotas.Sum(c => c.MontoInteres - c.MontoPagadoInteres));
            decimal moraPendiente = prestamos.Where(p => p.Status != "Pagado").Sum(p => p.Cuotas.Sum(c => c.MontoMoratorio - c.MontoPagadoMora));
            int clientesMoraActiva = prestamos.Count(p => p.Status == "Mora");

            return new ResumenCarteraDto
            {
                CapitalColocado = capitalColocado,
                InteresPendiente = interesPendiente,
                TotalProyectado = capitalColocado + interesPendiente + moraPendiente,
                ClientesMoraActiva = clientesMoraActiva,
                CapitalHistoricoPrestado = capitalHistorico,
                CapitalActual = capitalActual
            };
        }

        public async Task<IngresosReporteDto> GetIngresosReporteAsync(DateTime startDate, DateTime endDate)
        {
            var pagos = await _context.Pagos
                .Where(p => p.FechaPago >= startDate && p.FechaPago <= endDate)
                .ToListAsync();

            decimal total = pagos.Sum(p => p.Monto);
            decimal capital = pagos.Sum(p => p.MontoPrincipal);
            decimal interes = pagos.Sum(p => p.MontoInteres);
            decimal mora = pagos.Sum(p => p.MontoMora);

            return new IngresosReporteDto
            {
                Total = total,
                Capital = capital,
                Interes = interes,
                Mora = mora
            };
        }

        public async Task<List<MoraDeudorDto>> GetDeudoresMoraAsync()
        {
            var hoy = DateTime.UtcNow;
            var moraList = new List<MoraDeudorDto>();

            var prestamosActivos = await _context.Prestamos
                .Include(p => p.Cliente)
                .Include(p => p.Cuotas)
                .Where(p => p.Status != "Pagado")
                .ToListAsync();

            foreach (var p in prestamosActivos)
            {
                PrestaFlow.API.Features.Pagos.PagosService.ActualizarMoraYRecalculos(p);

                var cuotasVencidas = p.Cuotas
                    .Where(c => (c.Estado == "Vencido" || c.FechaVencimiento < hoy) && c.Estado != "Pagado")
                    .ToList();

                if (cuotasVencidas.Any())
                {
                    int cuotasVencidasCount = cuotasVencidas.Count;
                    decimal montoAtrasado = cuotasVencidas.Sum(c => 
                        (c.MontoPrincipal - c.MontoPagadoPrincipal) + 
                        (c.MontoInteres - c.MontoPagadoInteres) + 
                        (c.MontoMoratorio - c.MontoPagadoMora));
                    
                    var oldestCuota = cuotasVencidas.OrderBy(c => c.FechaVencimiento).First();
                    int diasDeRetraso = (hoy - oldestCuota.FechaVencimiento).Days;

                    string nivelRiesgo = "Bajo";
                    if (diasDeRetraso > 30) nivelRiesgo = "Alto";
                    else if (diasDeRetraso > 15) nivelRiesgo = "Medio";

                    moraList.Add(new MoraDeudorDto
                    {
                        ClienteNombre = p.Cliente.Nombre,
                        ClienteIdentidad = p.Cliente.Identidad,
                        PrestamoCodigo = $"PF-{p.Id:D4}",
                        CuotasVencidas = cuotasVencidasCount,
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
