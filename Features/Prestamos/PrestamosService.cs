using PrestaFlow.API.Data;
using PrestaFlow.API.Entities;
using PrestaFlow.API.Features.Prestamos.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrestaFlow.API.Features.Prestamos
{
    public class PrestamosService
    {
        private readonly PrestaFlowDbContext _context;

        public PrestamosService(PrestaFlowDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todos los préstamos registrados en el sistema, ordenados por fecha de otorgamiento descendente.
        /// </summary>
        public async Task<List<PrestamoResponseDto>> GetPrestamosAsync()
        {
            var prestamos = await _context.Prestamos
                .Include(p => p.Cliente)
                .Include(p => p.Cuotas)
                .OrderByDescending(p => p.FechaOtorgado)
                .ToListAsync();

            foreach (var p in prestamos)
            {
                PrestaFlow.API.Features.Pagos.PagosService.ActualizarMoraYRecalculos(p);
            }

            return prestamos.Select(MapToResponseDto).ToList();
        }

        /// <summary>
        /// Registra un nuevo préstamo en el sistema, realizando el desembolso contable desde una cuenta financiera.
        /// </summary>
        public async Task<PrestamoResponseDto> CrearPrestamoAsync(PrestamoCreateDto dto)
        {
            // 1. Validar cliente
            var cliente = await _context.Clientes.FindAsync(dto.ClienteId);
            if (cliente == null)
            {
                throw new InvalidOperationException($"El cliente con ID '{dto.ClienteId}' no existe.");
            }

            // 2. Validar cuenta de desembolso
            var cuenta = await _context.CuentasFinancieras.FindAsync(dto.CuentaDesembolsoId);
            if (cuenta == null)
            {
                throw new InvalidOperationException($"La cuenta de desembolso con ID '{dto.CuentaDesembolsoId}' no existe.");
            }

            // 3. Verificar fondos suficientes
            if (cuenta.Saldo < dto.Capital)
            {
                throw new InvalidOperationException($"Fondos insuficientes en la cuenta '{cuenta.Nombre}' para realizar el desembolso de L. {dto.Capital:N2} (Saldo disponible: L. {cuenta.Saldo:N2}).");
            }

            // 4. Calcular cuotas utilizando el método de interés simple (Capital + Interés) / Plazo
            decimal totalInteres = dto.Capital * (dto.InteresPorcentaje / 100m);
            decimal totalCobrar = dto.Capital + totalInteres;
            decimal cuotaMonto = Math.Round(totalCobrar / dto.PlazoCuotas, 2);

            // 5. Iniciar transacción en base de datos para asegurar atomicidad
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // A. Crear registro de préstamo
                var prestamo = new Prestamo
                {
                    ClienteId = dto.ClienteId,
                    Capital = dto.Capital,
                    InteresPorcentaje = dto.InteresPorcentaje,
                    PlazoCuotas = dto.PlazoCuotas,
                    CuotaMonto = cuotaMonto,
                    CuotasPagadas = 0,
                    Status = "Activo",
                    Frecuencia = dto.Frecuencia,
                    TipoPrestamo = dto.TipoPrestamo,
                    MetodoDesembolso = dto.MetodoDesembolso,
                    TipoInteres = dto.TipoInteres,
                    TasaMoraPorcentaje = dto.TasaMoraPorcentaje,
                    FechaOtorgado = DateTime.UtcNow
                };

                // Generar cronograma de cuotas físicas
                decimal principalAcumulado = 0m;
                decimal interesAcumulado = 0m;

                decimal principalPorCuota = Math.Round(dto.Capital / dto.PlazoCuotas, 2);
                decimal interesPorCuota = Math.Round(totalInteres / dto.PlazoCuotas, 2);

                for (int i = 1; i <= dto.PlazoCuotas; i++)
                {
                    decimal pMonto = principalPorCuota;
                    decimal iMonto = interesPorCuota;

                    // Ajuste de redondeo en la última cuota
                    if (i == dto.PlazoCuotas)
                    {
                        pMonto = dto.Capital - principalAcumulado;
                        iMonto = totalInteres - interesAcumulado;
                    }

                    principalAcumulado += pMonto;
                    interesAcumulado += iMonto;

                    DateTime fechaVencimiento = DateTime.UtcNow;
                    if (dto.Frecuencia == "Diario")
                        fechaVencimiento = fechaVencimiento.AddDays(i);
                    else if (dto.Frecuencia == "Semanal")
                        fechaVencimiento = fechaVencimiento.AddDays(i * 7);
                    else if (dto.Frecuencia == "Mensual")
                        fechaVencimiento = fechaVencimiento.AddMonths(i);

                    prestamo.Cuotas.Add(new Cuota
                    {
                        NumeroCuota = i,
                        FechaVencimiento = fechaVencimiento,
                        MontoPrincipal = pMonto,
                        MontoInteres = iMonto,
                        MontoMoratorio = 0.00m,
                        MontoPagadoPrincipal = 0.00m,
                        MontoPagadoInteres = 0.00m,
                        MontoPagadoMora = 0.00m,
                        Estado = "Pendiente",
                        FechaUltimoCalculoMora = DateTime.UtcNow
                    });
                }

                await _context.Prestamos.AddAsync(prestamo);
                await _context.SaveChangesAsync(); // Generar ID del préstamo y sus cuotas

                // B. Descontar saldo de la cuenta de desembolso
                cuenta.Saldo -= dto.Capital;

                // C. Registrar movimiento en la tesorería (Caja/Banco)
                var transaccionCaja = new TransaccionFinanciera
                {
                    CuentaId = dto.CuentaDesembolsoId,
                    Tipo = "Egreso",
                    Monto = dto.Capital,
                    Concepto = $"[Desembolso - {dto.MetodoDesembolso}] Préstamo PF-{prestamo.Id:0000} a {cliente.Nombre}",
                    Fecha = DateTime.UtcNow
                };

                await _context.TransaccionesFinancieras.AddAsync(transaccionCaja);
                await _context.SaveChangesAsync();

                // Confirmar transacción
                await transaction.CommitAsync();

                // Recargar para navegación
                await _context.Entry(prestamo).Reference(p => p.Cliente).LoadAsync();
                await _context.Entry(prestamo).Collection(p => p.Cuotas).LoadAsync();

                return MapToResponseDto(prestamo);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Mapea la entidad Prestamo a su DTO de respuesta.
        /// </summary>
        private PrestamoResponseDto MapToResponseDto(Prestamo p)
        {
            return new PrestamoResponseDto
            {
                Id = p.Id,
                Codigo = $"PF-{p.Id:0000}",
                ClienteId = p.ClienteId,
                ClienteNombre = p.Cliente.Nombre,
                ClientePhone = p.Cliente.Phone,
                Capital = p.Capital,
                InteresPorcentaje = p.InteresPorcentaje,
                PlazoCuotas = p.PlazoCuotas,
                CuotaMonto = p.CuotaMonto,
                CuotasPagadas = p.CuotasPagadas,
                Status = p.Status,
                Frecuencia = p.Frecuencia,
                TipoPrestamo = p.TipoPrestamo,
                MetodoDesembolso = p.MetodoDesembolso,
                TipoInteres = p.TipoInteres,
                TasaMoraPorcentaje = p.TasaMoraPorcentaje,
                FechaOtorgado = p.FechaOtorgado,
                Cuotas = p.Cuotas.OrderBy(c => c.NumeroCuota).Select(c => new CuotaResponseDto
                {
                    Id = c.Id,
                    NumeroCuota = c.NumeroCuota,
                    FechaVencimiento = c.FechaVencimiento,
                    MontoPrincipal = c.MontoPrincipal,
                    MontoInteres = c.MontoInteres,
                    MontoMoratorio = c.MontoMoratorio,
                    MontoPagadoPrincipal = c.MontoPagadoPrincipal,
                    MontoPagadoInteres = c.MontoPagadoInteres,
                    MontoPagadoMora = c.MontoPagadoMora,
                    Estado = c.Estado,
                    FechaUltimoCalculoMora = c.FechaUltimoCalculoMora
                }).ToList()
            };
        }

        /// <summary>
        /// Capitaliza el interés no pagado de una cuota en mora, sumándolo al capital principal del préstamo y recalculando las cuotas futuras.
        /// </summary>
        public async Task CapitalizarInteresAsync(int prestamoId, int cuotaId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var prestamo = await _context.Prestamos
                    .Include(p => p.Cuotas)
                    .FirstOrDefaultAsync(p => p.Id == prestamoId);

                if (prestamo == null)
                    throw new KeyNotFoundException($"El préstamo con ID {prestamoId} no existe.");

                var cuotaACapitalizar = prestamo.Cuotas.FirstOrDefault(c => c.Id == cuotaId);
                if (cuotaACapitalizar == null)
                    throw new KeyNotFoundException($"La cuota con ID {cuotaId} no pertenece a este préstamo.");

                decimal interesACapitalizar = cuotaACapitalizar.MontoInteres - cuotaACapitalizar.MontoPagadoInteres;
                if (interesACapitalizar <= 0)
                    throw new InvalidOperationException("Esta cuota no tiene intereses pendientes para capitalizar.");

                // Las cuotas futuras que recibirán la distribución
                var cuotasFuturas = prestamo.Cuotas
                    .Where(c => c.Estado != "Pagado" && c.Id != cuotaId && c.NumeroCuota > cuotaACapitalizar.NumeroCuota)
                    .OrderBy(c => c.NumeroCuota)
                    .ToList();

                int N = cuotasFuturas.Count;
                if (N == 0)
                    throw new InvalidOperationException("No existen cuotas futuras pendientes para distribuir el interés capitalizado.");

                // 1. Reducir el interés pendiente de la cuota origen a 0 (ya que se capitaliza)
                cuotaACapitalizar.MontoInteres = cuotaACapitalizar.MontoPagadoInteres;
                
                // Si ya se pagó el principal de esta cuota origen, marcarla como Pagada
                if (cuotaACapitalizar.MontoPrincipal <= cuotaACapitalizar.MontoPagadoPrincipal)
                {
                    cuotaACapitalizar.Estado = "Pagado";
                }

                // 2. Incrementar el capital principal del préstamo
                prestamo.Capital += interesACapitalizar;

                // 3. Calcular los montos adicionales por cuota futura
                decimal adicionalPrincipalPorCuota = Math.Round(interesACapitalizar / N, 2);
                decimal adicionalInteresPorCuota = Math.Round((interesACapitalizar * (prestamo.InteresPorcentaje / 100m)) / N, 2);

                decimal principalAcumulado = 0m;
                decimal interesAcumulado = 0m;
                decimal totalInteresAdicional = interesACapitalizar * (prestamo.InteresPorcentaje / 100m);

                for (int i = 0; i < N; i++)
                {
                    var c = cuotasFuturas[i];
                    
                    decimal pAdd = adicionalPrincipalPorCuota;
                    decimal iAdd = adicionalInteresPorCuota;

                    // Ajuste de redondeo en la última cuota futura
                    if (i == N - 1)
                    {
                        pAdd = interesACapitalizar - principalAcumulado;
                        iAdd = totalInteresAdicional - interesAcumulado;
                    }

                    principalAcumulado += pAdd;
                    interesAcumulado += iAdd;

                    c.MontoPrincipal += pAdd;
                    c.MontoInteres += iAdd;
                }

                // 4. Actualizar el monto sugerido de la cuota del préstamo
                var primeraFutura = cuotasFuturas.FirstOrDefault();
                if (primeraFutura != null)
                {
                    prestamo.CuotaMonto = primeraFutura.MontoPrincipal + primeraFutura.MontoInteres;
                }

                _context.Prestamos.Update(prestamo);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
