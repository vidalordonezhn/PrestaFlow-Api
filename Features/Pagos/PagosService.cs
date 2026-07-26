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
                .FirstOrDefaultAsync(p => p.Id == dto.PrestamoId);

            if (prestamo == null)
            {
                throw new InvalidOperationException($"El préstamo con ID '{dto.PrestamoId}' no existe.");
            }

            if (prestamo.Status == "Pagado")
            {
                throw new InvalidOperationException($"El préstamo PF-{prestamo.Id:0000} ya ha sido cancelado por completo.");
            }

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

                await _context.Pagos.AddAsync(pago);
                await _context.SaveChangesAsync(); // Generar ID del pago

                // B. Actualizar cuotas pagadas y estado en el préstamo
                prestamo.CuotasPagadas += 1;
                if (prestamo.CuotasPagadas >= prestamo.PlazoCuotas)
                {
                    prestamo.Status = "Pagado";
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
                FechaPago = p.FechaPago,
                MetodoPago = p.MetodoPago,
                Referencia = p.Referencia,
                CreadoPor = p.CreadoPor ?? "admin"
            };
        }
    }
}
