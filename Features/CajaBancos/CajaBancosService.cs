using PrestaFlow.API.Data;
using PrestaFlow.API.Entities;
using PrestaFlow.API.Features.CajaBancos.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrestaFlow.API.Features.CajaBancos
{
    public class CajaBancosService
    {
        private readonly PrestaFlowDbContext _context;

        public CajaBancosService(PrestaFlowDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todas las cuentas financieras activas (Cajas y Bancos).
        /// </summary>
        public async Task<List<CuentaResponseDto>> GetCuentasAsync()
        {
            var cuentas = await _context.CuentasFinancieras
                .OrderBy(c => c.Tipo)
                .ThenBy(c => c.Nombre)
                .ToListAsync();

            return cuentas.Select(c => new CuentaResponseDto
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Tipo = c.Tipo,
                Saldo = c.Saldo
            }).ToList();
        }

        /// <summary>
        /// Crea una nueva cuenta financiera (Caja o Banco) con un saldo inicial y su correspondiente transacción de apertura.
        /// </summary>
        public async Task<CuentaResponseDto> CrearCuentaAsync(CuentaCreateDto dto)
        {
            var cuenta = new CuentaFinanciera
            {
                Nombre = dto.Nombre,
                Tipo = dto.Tipo,
                Saldo = dto.Saldo
            };

            await _context.CuentasFinancieras.AddAsync(cuenta);
            await _context.SaveChangesAsync(); // Guardamos para obtener el Id autogenerado

            if (dto.Saldo > 0)
            {
                var transaccion = new TransaccionFinanciera
                {
                    CuentaId = cuenta.Id,
                    Tipo = "Ingreso",
                    Monto = dto.Saldo,
                    Concepto = $"Saldo inicial de apertura de la cuenta {cuenta.Nombre}",
                    Fecha = DateTime.UtcNow
                };
                await _context.TransaccionesFinancieras.AddAsync(transaccion);
                await _context.SaveChangesAsync();
            }

            return new CuentaResponseDto
            {
                Id = cuenta.Id,
                Nombre = cuenta.Nombre,
                Tipo = cuenta.Tipo,
                Saldo = cuenta.Saldo
            };
        }

        /// <summary>
        /// Obtiene todo el historial de transacciones financieras ordenadas por fecha descendente.
        /// </summary>
        public async Task<List<TransaccionResponseDto>> GetTransaccionesAsync()
        {
            var transacciones = await _context.TransaccionesFinancieras
                .Include(t => t.Cuenta)
                .OrderByDescending(t => t.Fecha)
                .ToListAsync();

            return transacciones.Select(MapToResponseDto).ToList();
        }

        /// <summary>
        /// Registra un ajuste de saldo manual (Depósito o Retiro) en una cuenta.
        /// </summary>
        public async Task<TransaccionResponseDto> RegistrarTransaccionAsync(TransaccionCreateDto dto)
        {
            var cuenta = await _context.CuentasFinancieras.FindAsync(dto.CuentaId);
            if (cuenta == null)
            {
                throw new InvalidOperationException($"La cuenta financiera con ID '{dto.CuentaId}' no existe.");
            }

            // Validar fondos si es Egreso
            if (dto.Tipo == "Egreso" && cuenta.Saldo < dto.Monto)
            {
                throw new InvalidOperationException($"Fondos insuficientes en la cuenta '{cuenta.Nombre}'. Saldo actual: L. {cuenta.Saldo}");
            }

            // Aplicar ajuste de saldo
            if (dto.Tipo == "Ingreso")
            {
                cuenta.Saldo += dto.Monto;
            }
            else
            {
                cuenta.Saldo -= dto.Monto;
            }

            var transaccion = new TransaccionFinanciera
            {
                CuentaId = dto.CuentaId,
                Tipo = dto.Tipo,
                Monto = dto.Monto,
                Concepto = dto.Concepto,
                Fecha = DateTime.UtcNow
            };

            await _context.TransaccionesFinancieras.AddAsync(transaccion);
            await _context.SaveChangesAsync();

            // Recargar entidad para mapeo auditado correcto
            await _context.Entry(transaccion).Reference(t => t.Cuenta).LoadAsync();

            return MapToResponseDto(transaccion);
        }

        /// <summary>
        /// Registra una transferencia de saldo entre dos cuentas financieras.
        /// </summary>
        public async Task<List<TransaccionResponseDto>> RegistrarTransferenciaAsync(TransferenciaCreateDto dto)
        {
            if (dto.CuentaOrigenId == dto.CuentaDestinoId)
            {
                throw new InvalidOperationException("La cuenta de origen y destino no pueden ser la misma.");
            }

            var cuentaOrigen = await _context.CuentasFinancieras.FindAsync(dto.CuentaOrigenId);
            var cuentaDestino = await _context.CuentasFinancieras.FindAsync(dto.CuentaDestinoId);

            if (cuentaOrigen == null || cuentaDestino == null)
            {
                throw new InvalidOperationException("Una o ambas cuentas financieras especificadas no existen.");
            }

            // Validar fondos en origen
            if (cuentaOrigen.Saldo < dto.Monto)
            {
                throw new InvalidOperationException($"Fondos insuficientes en la cuenta de origen '{cuentaOrigen.Nombre}'. Saldo actual: L. {cuentaOrigen.Saldo}");
            }

            // Transferir saldo
            cuentaOrigen.Saldo -= dto.Monto;
            cuentaDestino.Saldo += dto.Monto;

            var fecha = DateTime.UtcNow;

            // Transacción 1: Débito en origen
            var txOrigen = new TransaccionFinanciera
            {
                CuentaId = dto.CuentaOrigenId,
                Tipo = "Transferencia",
                Monto = dto.Monto,
                Concepto = $"[Egreso] A {cuentaDestino.Nombre}: {dto.Concepto}",
                Fecha = fecha
            };

            // Transacción 2: Crédito en destino
            var txDestino = new TransaccionFinanciera
            {
                CuentaId = dto.CuentaDestinoId,
                Tipo = "Transferencia",
                Monto = dto.Monto,
                Concepto = $"[Ingreso] Desde {cuentaOrigen.Nombre}: {dto.Concepto}",
                Fecha = fecha
            };

            await _context.TransaccionesFinancieras.AddRangeAsync(txOrigen, txDestino);
            await _context.SaveChangesAsync();

            // Recargar referencias
            await _context.Entry(txOrigen).Reference(t => t.Cuenta).LoadAsync();
            await _context.Entry(txDestino).Reference(t => t.Cuenta).LoadAsync();

            return new List<TransaccionResponseDto>
            {
                MapToResponseDto(txOrigen),
                MapToResponseDto(txDestino)
            };
        }

        /// <summary>
        /// Mapea una entidad TransaccionFinanciera a su DTO de respuesta.
        /// </summary>
        private TransaccionResponseDto MapToResponseDto(TransaccionFinanciera t)
        {
            return new TransaccionResponseDto
            {
                Id = t.Id,
                CuentaId = t.CuentaId,
                CuentaNombre = t.Cuenta.Nombre,
                Tipo = t.Tipo,
                Monto = t.Monto,
                Concepto = t.Concepto,
                Fecha = t.Fecha,
                CreadoPor = t.CreadoPor
            };
        }
    }
}
