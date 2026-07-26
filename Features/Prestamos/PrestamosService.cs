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
                .OrderByDescending(p => p.FechaOtorgado)
                .ToListAsync();

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
                    FechaOtorgado = DateTime.UtcNow
                };

                await _context.Prestamos.AddAsync(prestamo);
                await _context.SaveChangesAsync(); // Generar ID del préstamo

                // B. Descontar saldo de la cuenta de desembolso
                cuenta.Saldo -= dto.Capital;

                // C. Registrar movimiento en la tesorería (Caja/Banco)
                var transaccionCaja = new TransaccionFinanciera
                {
                    CuentaId = dto.CuentaDesembolsoId,
                    Tipo = "Egreso",
                    Monto = dto.Capital,
                    Concepto = $"[Desembolso] Préstamo PF-{prestamo.Id:0000} a {cliente.Nombre}",
                    Fecha = DateTime.UtcNow
                };

                await _context.TransaccionesFinancieras.AddAsync(transaccionCaja);
                await _context.SaveChangesAsync();

                // Confirmar transacción
                await transaction.CommitAsync();

                // Recargar para navegación
                await _context.Entry(prestamo).Reference(p => p.Cliente).LoadAsync();

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
                FechaOtorgado = p.FechaOtorgado
            };
        }
    }
}
