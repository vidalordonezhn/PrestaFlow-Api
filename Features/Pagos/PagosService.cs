using PrestaFlow.API.Data;
using PrestaFlow.API.Entities;
using PrestaFlow.API.Features.Pagos.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrestaFlow.API.Features.Pagos
{
    public class PagosService
    {
        private readonly PrestaFlowDbContext _context;

        public PagosService(PrestaFlowDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene el historial de todos los pagos registrados en orden cronológico descendente.
        /// </summary>
        public async Task<List<PagoResponseDto>> GetPagosAsync()
        {
            var pagos = await _context.Pagos
                .Include(p => p.Prestamo)
                    .ThenInclude(pr => pr.Cliente)
                .OrderByDescending(p => p.FechaPago)
                .ToListAsync();

            return pagos.Select(MapToResponseDto).ToList();
        }

        /// <summary>
        /// Registra un nuevo abono/pago a un préstamo, aumentando el saldo de la caja/banco correspondiente y registrando la entrada contable.
        /// </summary>
        public async Task<PagoResponseDto> CrearPagoAsync(PagoCreateDto dto)
        {
            // 1. Validar préstamo
            var prestamo = await _context.Prestamos
                .Include(p => p.Cliente)
                .Include(p => p.Cuotas)
                .FirstOrDefaultAsync(p => p.Id == dto.PrestamoId);

            if (prestamo == null)
            {
                throw new InvalidOperationException($"El préstamo con ID '{dto.PrestamoId}' no existe.");
            }

            if (prestamo.Status == "Pagado")
            {
                throw new InvalidOperationException($"El préstamo PF-{prestamo.Id:0000} ya ha sido cancelado por completo.");
            }

            // Actualizar la mora de las cuotas vencidas y recalcular saldos variables antes de abonar
            ActualizarMoraYRecalculos(prestamo);

            // 2. Determinar la cuenta financiera de destino (Efectivo -> Caja Chica General, Transferencia -> Banco Atlántida)
            int cuentaId = dto.MetodoPago == "Efectivo" ? 1 : 2;
            var cuenta = await _context.CuentasFinancieras.FindAsync(cuentaId);
            if (cuenta == null)
            {
                throw new InvalidOperationException($"La cuenta contable de destino para {dto.MetodoPago} (ID: {cuentaId}) no está configurada.");
            }

            // 3. Ejecutar de forma atómica en una transacción
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // A. Crear registro de pago
                var pago = new Pago
                {
                    PrestamoId = dto.PrestamoId,
                    Monto = dto.Monto,
                    FechaPago = DateTime.UtcNow,
                    MetodoPago = dto.MetodoPago,
                    Referencia = dto.Referencia
                };

                // Distribuir el abono en cascada (Waterfall)
                decimal restante = dto.Monto;
                decimal abonadoPrincipal = 0m;
                decimal abonadoInteres = 0m;
                decimal abonadoMora = 0m;

                var cuotasPendientes = prestamo.Cuotas
                    .Where(c => c.Estado != "Pagado")
                    .OrderBy(c => c.NumeroCuota)
                    .ToList();

                foreach (var cuota in cuotasPendientes)
                {
                    if (restante <= 0m) break;

                    // 1. Cobrar Mora primero
                    decimal moraPorPagar = cuota.MontoMoratorio - cuota.MontoPagadoMora;
                    if (moraPorPagar > 0m)
                    {
                        decimal pagoMora = Math.Min(moraPorPagar, restante);
                        cuota.MontoPagadoMora += pagoMora;
                        abonadoMora += pagoMora;
                        restante -= pagoMora;
                    }

                    if (restante <= 0m) break;

                    // 2. Cobrar Interés
                    decimal interesPorPagar = cuota.MontoInteres - cuota.MontoPagadoInteres;
                    if (interesPorPagar > 0m)
                    {
                        decimal pagoInteres = Math.Min(interesPorPagar, restante);
                        cuota.MontoPagadoInteres += pagoInteres;
                        abonadoInteres += pagoInteres;
                        restante -= pagoInteres;
                    }

                    if (restante <= 0m) break;

                    // 3. Cobrar Principal
                    decimal principalPorPagar = cuota.MontoPrincipal - cuota.MontoPagadoPrincipal;
                    if (principalPorPagar > 0m)
                    {
                        decimal pagoPrincipal = Math.Min(principalPorPagar, restante);
                        cuota.MontoPagadoPrincipal += pagoPrincipal;
                        abonadoPrincipal += pagoPrincipal;
                        restante -= pagoPrincipal;
                    }

                    // Actualizar estado de la cuota individual
                    decimal pagadoTotalPeriodo = cuota.MontoPagadoPrincipal + cuota.MontoPagadoInteres + cuota.MontoPagadoMora;
                    decimal debidoTotalPeriodo = cuota.MontoPrincipal + cuota.MontoInteres + cuota.MontoMoratorio;
                    decimal diferencia = debidoTotalPeriodo - pagadoTotalPeriodo;

                    if (diferencia <= 0.05m) // Tolerancia para diferencias de centavos por división
                    {
                        if (diferencia > 0m)
                        {
                            cuota.MontoPagadoPrincipal += diferencia;
                            abonadoPrincipal += diferencia;
                        }
                        cuota.Estado = "Pagado";
                    }
                    else
                    {
                        cuota.Estado = "Parcial";
                    }
                }

                // Asignar los montos desglosados al registro de Pago
                pago.MontoPrincipal = abonadoPrincipal;
                pago.MontoInteres = abonadoInteres;
                pago.MontoMora = abonadoMora;

                await _context.Pagos.AddAsync(pago);
                await _context.SaveChangesAsync(); // Generar ID del pago

                // B. Actualizar cuotas pagadas y estado en el préstamo
                prestamo.CuotasPagadas = prestamo.Cuotas.Count(c => c.Estado == "Pagado");
                if (prestamo.CuotasPagadas >= prestamo.PlazoCuotas)
                {
                    prestamo.Status = "Pagado";
                }
                else
                {
                    bool tieneMora = prestamo.Cuotas.Any(c => c.Estado == "Vencido" || (c.FechaVencimiento < DateTime.UtcNow && c.Estado != "Pagado"));
                    prestamo.Status = tieneMora ? "Mora" : "Activo";
                }

                // C. Incrementar saldo de la cuenta receptora de fondos
                cuenta.Saldo += dto.Monto;

                // D. Registrar el ingreso en el diario de tesorería
                var transaccionCaja = new TransaccionFinanciera
                {
                    CuentaId = cuentaId,
                    Tipo = "Ingreso",
                    Monto = dto.Monto,
                    Concepto = $"[Abono] Pago cuota préstamo PF-{prestamo.Id:0000} - Cliente: {prestamo.Cliente.Nombre}",
                    Fecha = DateTime.UtcNow
                };

                await _context.TransaccionesFinancieras.AddAsync(transaccionCaja);
                await _context.SaveChangesAsync();

                // Confirmar cambios
                await transaction.CommitAsync();

                return MapToResponseDto(pago);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Recalcula de forma dinámica los intereses moratorios y actualiza la tasa variable según saldos caídos.
        /// </summary>
        public static void ActualizarMoraYRecalculos(Prestamo prestamo)
        {
            // 1. Calcular intereses moratorios para cuotas vencidas
            foreach (var c in prestamo.Cuotas.Where(c => c.Estado != "Pagado"))
            {
                if (c.FechaVencimiento < DateTime.UtcNow)
                {
                    int diasRetraso = (DateTime.UtcNow - c.FechaVencimiento).Days;
                    if (diasRetraso > 0)
                    {
                        decimal capitalVencido = c.MontoPrincipal - c.MontoPagadoPrincipal;
                        decimal tasaDiaria = (prestamo.TasaMoraPorcentaje / 100m) / 30m; // Tasa mensual / 30
                        c.MontoMoratorio = Math.Round(capitalVencido * tasaDiaria * diasRetraso, 2);
                        c.Estado = "Vencido";
                    }
                }
            }

            // 2. Si es interés variable, recalcular el interés sobre saldos caídos para cuotas futuras
            if (prestamo.TipoInteres == "Variable")
            {
                decimal capitalPagadoTotal = prestamo.Cuotas.Sum(c => c.MontoPagadoPrincipal);
                decimal capitalPendienteTotal = Math.Max(0m, prestamo.Capital - capitalPagadoTotal);
                
                var cuotasFuturas = prestamo.Cuotas
                    .Where(c => c.Estado == "Pendiente" && c.FechaVencimiento > DateTime.UtcNow)
                    .OrderBy(c => c.NumeroCuota)
                    .ToList();

                if (cuotasFuturas.Any())
                {
                    decimal interesTotalRestante = capitalPendienteTotal * (prestamo.InteresPorcentaje / 100m);
                    decimal interesPorCuotaFutura = Math.Round(interesTotalRestante / cuotasFuturas.Count, 2);

                    foreach (var cf in cuotasFuturas)
                    {
                        cf.MontoInteres = interesPorCuotaFutura;
                    }
                }
            }
        }

        /// <summary>
        /// Mapea la entidad Pago a su DTO de respuesta.
        /// </summary>
        private PagoResponseDto MapToResponseDto(Pago p)
        {
            return new PagoResponseDto
            {
                Id = p.Id,
                PrestamoId = p.PrestamoId,
                PrestamoCodigo = $"PF-{p.PrestamoId:0000}",
                ClienteNombre = p.Prestamo.Cliente.Nombre,
                ClienteIdentidad = p.Prestamo.Cliente.Identidad,
                ClientePhone = p.Prestamo.Cliente.Phone,
                Monto = p.Monto,
                MontoPrincipal = p.MontoPrincipal,
                MontoInteres = p.MontoInteres,
                MontoMora = p.MontoMora,
                FechaPago = p.FechaPago,
                MetodoPago = p.MetodoPago,
                Referencia = p.Referencia,
                CreadoPor = p.CreadoPor ?? "admin"
            };
        }
    }
}
