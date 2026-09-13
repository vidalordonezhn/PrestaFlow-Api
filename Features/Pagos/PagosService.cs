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

                // Distribuir el abono
                decimal restante = dto.Monto;
                decimal abonadoPrincipal = 0m;
                decimal abonadoInteres = 0m;
                decimal abonadoMora = 0m;

                var cuotasPendientes = prestamo.Cuotas
                    .Where(c => c.Estado != "Pagado")
                    .OrderBy(c => c.NumeroCuota)
                    .ToList();

                if (dto.EsAbonoCapital)
                {
                    // 1. Cobrar primero la cuota exigible actual / vencida (Mora -> Interés -> Principal)
                    var cuotaExigible = cuotasPendientes.FirstOrDefault();
                    if (cuotaExigible != null)
                    {
                        // A. Mora
                        decimal moraPorPagar = cuotaExigible.MontoMoratorio - cuotaExigible.MontoPagadoMora;
                        if (moraPorPagar > 0m && restante > 0m)
                        {
                            decimal pagoMora = Math.Min(moraPorPagar, restante);
                            cuotaExigible.MontoPagadoMora += pagoMora;
                            abonadoMora += pagoMora;
                            restante -= pagoMora;
                        }

                        // B. Interés
                        decimal interesPorPagar = cuotaExigible.MontoInteres - cuotaExigible.MontoPagadoInteres;
                        if (interesPorPagar > 0m && restante > 0m)
                        {
                            decimal pagoInteres = Math.Min(interesPorPagar, restante);
                            cuotaExigible.MontoPagadoInteres += pagoInteres;
                            abonadoInteres += pagoInteres;
                            restante -= pagoInteres;
                        }

                        // C. Principal
                        decimal principalPorPagar = cuotaExigible.MontoPrincipal - cuotaExigible.MontoPagadoPrincipal;
                        if (principalPorPagar > 0m && restante > 0m)
                        {
                            decimal pagoPrincipal = Math.Min(principalPorPagar, restante);
                            cuotaExigible.MontoPagadoPrincipal += pagoPrincipal;
                            abonadoPrincipal += pagoPrincipal;
                            restante -= pagoPrincipal;
                        }

                        // Evaluar estado de la cuota exigible
                        decimal pagadoTotal = cuotaExigible.MontoPagadoPrincipal + cuotaExigible.MontoPagadoInteres + cuotaExigible.MontoPagadoMora;
                        decimal debidoTotal = cuotaExigible.MontoPrincipal + cuotaExigible.MontoInteres + cuotaExigible.MontoMoratorio;
                        if (debidoTotal - pagadoTotal <= 0.05m)
                        {
                            cuotaExigible.Estado = "Pagado";
                        }
                        else
                        {
                            cuotaExigible.Estado = "Parcial";
                        }
                    }

                    // 2. Todo el excedente se abona DIRECTO A CAPITAL (Principal) reduciendo cuotas futuras desde la última
                    if (restante > 0m)
                    {
                        var cuotasParaAmortizar = prestamo.Cuotas
                            .Where(c => c.Estado != "Pagado")
                            .OrderByDescending(c => c.NumeroCuota)
                            .ToList();

                        foreach (var cuota in cuotasParaAmortizar)
                        {
                            if (restante <= 0m) break;

                            decimal capitalPendiente = cuota.MontoPrincipal - cuota.MontoPagadoPrincipal;
                            if (capitalPendiente > 0m)
                            {
                                decimal pagoCap = Math.Min(capitalPendiente, restante);
                                cuota.MontoPagadoPrincipal += pagoCap;
                                abonadoPrincipal += pagoCap;
                                restante -= pagoCap;

                                // Si se amortizó por completo el principal de esta cuota, se exonera su interés futuro y se marca Pagada
                                if (cuota.MontoPrincipal - cuota.MontoPagadoPrincipal <= 0.05m)
                                {
                                    cuota.MontoInteres = cuota.MontoPagadoInteres; // Exonera interés no devengado
                                    cuota.Estado = "Pagado";
                                }
                                else
                                {
                                    cuota.Estado = "Parcial";
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Modalidad estándar en Cascada Waterfall
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
                }

                // Asignar los montos desglosados al registro de Pago
                pago.MontoPrincipal = abonadoPrincipal;
                pago.MontoInteres = abonadoInteres;
                pago.MontoMora = abonadoMora;

                await _context.Pagos.AddAsync(pago);
                await _context.SaveChangesAsync(); // Generar ID del pago

                // B. Actualizar cuotas pagadas y estado en el préstamo
                prestamo.CuotasPagadas = prestamo.Cuotas.Count(c => c.Estado == "Pagado");
                decimal deudaPendienteTotal = prestamo.Cuotas.Sum(c => (c.MontoPrincipal + c.MontoInteres + c.MontoMoratorio) - (c.MontoPagadoPrincipal + c.MontoPagadoInteres + c.MontoPagadoMora));
                if (prestamo.CuotasPagadas >= prestamo.PlazoCuotas || deudaPendienteTotal <= 0.05m)
                {
                    prestamo.Status = "Pagado";
                    prestamo.CuotasPagadas = prestamo.PlazoCuotas;
                    foreach (var c in prestamo.Cuotas)
                    {
                        c.Estado = "Pagado";
                    }
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
            if (prestamo.Cuotas == null || !prestamo.Cuotas.Any()) return;

            // 0. Si el préstamo ya está Pagado o todo el capital + interés original ha sido cubierto
            decimal capitalTotal = prestamo.Cuotas.Sum(c => c.MontoPrincipal);
            decimal interesTotal = prestamo.Cuotas.Sum(c => c.MontoInteres);
            decimal pagadoPrincipalTotal = prestamo.Cuotas.Sum(c => c.MontoPagadoPrincipal);
            decimal pagadoInteresTotal = prestamo.Cuotas.Sum(c => c.MontoPagadoInteres);
            decimal saldoPendiente = (capitalTotal + interesTotal) - (pagadoPrincipalTotal + pagadoInteresTotal);

            if (prestamo.Status == "Pagado" || saldoPendiente <= 0.05m)
            {
                prestamo.Status = "Pagado";
                prestamo.CuotasPagadas = prestamo.PlazoCuotas;
                foreach (var c in prestamo.Cuotas)
                {
                    c.Estado = "Pagado";
                }
                return;
            }

            // 1. Calcular intereses moratorios para cuotas vencidas
            foreach (var c in prestamo.Cuotas.Where(c => c.Estado != "Pagado"))
            {
                decimal debido = c.MontoPrincipal + c.MontoInteres + c.MontoMoratorio;
                decimal pagado = c.MontoPagadoPrincipal + c.MontoPagadoInteres + c.MontoPagadoMora;
                if (debido - pagado <= 0.05m)
                {
                    c.Estado = "Pagado";
                    continue;
                }

                if (c.FechaVencimiento < DateTime.UtcNow)
                {
                    int diasRetraso = (DateTime.UtcNow - c.FechaVencimiento).Days;
                    if (diasRetraso > 0)
                    {
                        decimal capitalVencido = c.MontoPrincipal - c.MontoPagadoPrincipal;
                        if (capitalVencido > 0)
                        {
                            decimal tasaDiaria = (prestamo.TasaMoraPorcentaje / 100m) / 30m; // Tasa mensual / 30
                            c.MontoMoratorio = Math.Round(capitalVencido * tasaDiaria * diasRetraso, 2);
                            c.Estado = "Vencido";
                        }
                    }
                }
            }

            // Actualizar cuotas pagadas y estado
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

            // 2. Si es interés variable, recalcular el interés sobre saldos caídos para cuotas futuras
            if (prestamo.TipoInteres == "Variable" && prestamo.Status != "Pagado")
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
        /// Anula un pago registrado previamente, revirtiendo el saldo de las cuotas, recalculando el préstamo y registrando el egreso contable.
        /// </summary>
        public async Task<PagoResponseDto> AnularPagoAsync(int pagoId, AnularPagoDto dto)
        {
            var pago = await _context.Pagos
                .Include(p => p.Prestamo)
                    .ThenInclude(pr => pr.Cliente)
                .Include(p => p.Prestamo)
                    .ThenInclude(pr => pr.Cuotas)
                .FirstOrDefaultAsync(p => p.Id == pagoId);

            if (pago == null)
            {
                throw new KeyNotFoundException($"El pago con ID {pagoId} no existe.");
            }

            if (pago.EsAnulado)
            {
                throw new InvalidOperationException($"El pago con ID {pagoId} ya fue anulado anteriormente.");
            }

            var prestamo = pago.Prestamo;
            int cuentaId = pago.MetodoPago == "Efectivo" ? 1 : 2;
            var cuenta = await _context.CuentasFinancieras.FindAsync(cuentaId);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Marcar pago como anulado
                pago.EsAnulado = true;
                pago.MotivoAnulacion = dto.Motivo;
                pago.FechaAnulacion = DateTime.UtcNow;

                // 2. Revertir montos aplicados a las cuotas
                decimal revMora = pago.MontoMora;
                decimal revInteres = pago.MontoInteres;
                decimal revPrincipal = pago.MontoPrincipal;

                var cuotasReverso = prestamo.Cuotas.OrderByDescending(c => c.NumeroCuota).ToList();

                // Revertir Principal
                foreach (var c in cuotasReverso)
                {
                    if (revPrincipal <= 0m) break;
                    decimal descuento = Math.Min(c.MontoPagadoPrincipal, revPrincipal);
                    c.MontoPagadoPrincipal -= descuento;
                    revPrincipal -= descuento;
                }

                // Revertir Interés
                foreach (var c in cuotasReverso)
                {
                    if (revInteres <= 0m) break;
                    decimal descuento = Math.Min(c.MontoPagadoInteres, revInteres);
                    c.MontoPagadoInteres -= descuento;
                    revInteres -= descuento;
                }

                // Revertir Mora
                foreach (var c in cuotasReverso)
                {
                    if (revMora <= 0m) break;
                    decimal descuento = Math.Min(c.MontoPagadoMora, revMora);
                    c.MontoPagadoMora -= descuento;
                    revMora -= descuento;
                }

                // Reevaluar estados individuales de las cuotas
                foreach (var c in prestamo.Cuotas)
                {
                    decimal pagado = c.MontoPagadoPrincipal + c.MontoPagadoInteres + c.MontoPagadoMora;
                    decimal debido = c.MontoPrincipal + c.MontoInteres + c.MontoMoratorio;

                    if (debido - pagado <= 0.05m)
                    {
                        c.Estado = "Pagado";
                    }
                    else if (pagado > 0m)
                    {
                        c.Estado = "Parcial";
                    }
                    else
                    {
                        c.Estado = c.FechaVencimiento < DateTime.UtcNow ? "Vencido" : "Pendiente";
                    }
                }

                // Recalcular mora y estado general del préstamo
                ActualizarMoraYRecalculos(prestamo);

                // 3. Ajustar saldo de la cuenta financiera receptora
                if (cuenta != null)
                {
                    cuenta.Saldo -= pago.Monto;

                    var transaccionCaja = new TransaccionFinanciera
                    {
                        CuentaId = cuentaId,
                        Tipo = "Egreso",
                        Monto = pago.Monto,
                        Concepto = $"[Anulación Pago #{pago.Id}] PF-{prestamo.Id:0000} - Motivo: {dto.Motivo}",
                        Fecha = DateTime.UtcNow
                    };

                    await _context.TransaccionesFinancieras.AddAsync(transaccionCaja);
                }

                await _context.SaveChangesAsync();
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
        /// Mapea la entidad Pago a su DTO de respuesta.
        /// </summary>
        private PagoResponseDto MapToResponseDto(Pago p)
        {
            return new PagoResponseDto
            {
                Id = p.Id,
                PrestamoId = p.PrestamoId,
                PrestamoCodigo = $"PF-{p.PrestamoId:0000}",
                ClienteNombre = p.Prestamo?.Cliente?.Nombre ?? "N/A",
                ClienteIdentidad = p.Prestamo?.Cliente?.Identidad ?? "N/A",
                ClientePhone = p.Prestamo?.Cliente?.Phone ?? "N/A",
                Monto = p.Monto,
                MontoPrincipal = p.MontoPrincipal,
                MontoInteres = p.MontoInteres,
                MontoMora = p.MontoMora,
                FechaPago = p.FechaPago,
                MetodoPago = p.MetodoPago,
                Referencia = p.Referencia,
                CreadoPor = p.CreadoPor ?? "admin",
                EsAnulado = p.EsAnulado,
                MotivoAnulacion = p.MotivoAnulacion,
                FechaAnulacion = p.FechaAnulacion
            };
        }
    }
}
