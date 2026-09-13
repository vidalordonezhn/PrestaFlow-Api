using PrestaFlow.API.Data;
using PrestaFlow.API.Entities;
using PrestaFlow.API.Features.Garantias.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrestaFlow.API.Features.Garantias
{
    public class GarantiasService
    {
        private readonly PrestaFlowDbContext _context;

        public GarantiasService(PrestaFlowDbContext context)
        {
            _context = context;
        }

        public async Task<List<GarantiaResponseDto>> GetGarantiasAsync(string? query = null, string? estado = null, string? tipo = null)
        {
            var q = _context.Garantias
                .Include(g => g.Cliente)
                .Include(g => g.Prestamo)
                    .ThenInclude(p => p!.Cuotas)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                var lower = query.Trim().ToLower();
                q = q.Where(g =>
                    g.Codigo.ToLower().Contains(lower) ||
                    g.Descripcion.ToLower().Contains(lower) ||
                    (g.NumeroSerie != null && g.NumeroSerie.ToLower().Contains(lower)) ||
                    g.Cliente.Nombre.ToLower().Contains(lower) ||
                    g.Cliente.Identidad.Contains(lower) ||
                    g.UbicacionFisica.ToLower().Contains(lower)
                );
            }

            if (!string.IsNullOrWhiteSpace(estado) && estado != "Todos")
            {
                q = q.Where(g => g.EstadoCustodia == estado);
            }

            if (!string.IsNullOrWhiteSpace(tipo) && tipo != "Todos")
            {
                q = q.Where(g => g.Tipo == tipo);
            }

            var list = await q.OrderByDescending(g => g.FechaIngreso).ToListAsync();
            return list.Select(MapToResponseDto).ToList();
        }

        public async Task<GarantiaResponseDto?> GetGarantiaByIdAsync(int id)
        {
            var garantia = await _context.Garantias
                .Include(g => g.Cliente)
                .Include(g => g.Prestamo)
                    .ThenInclude(p => p!.Cuotas)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (garantia == null) return null;
            return MapToResponseDto(garantia);
        }

        public async Task<List<GarantiaResponseDto>> GetGarantiasByClienteAsync(int clienteId)
        {
            var list = await _context.Garantias
                .Include(g => g.Cliente)
                .Include(g => g.Prestamo)
                    .ThenInclude(p => p!.Cuotas)
                .Where(g => g.ClienteId == clienteId)
                .OrderByDescending(g => g.FechaIngreso)
                .ToListAsync();

            return list.Select(MapToResponseDto).ToList();
        }

        public async Task<GarantiaResponseDto> CrearGarantiaAsync(CrearGarantiaDto dto)
        {
            var cliente = await _context.Clientes.FindAsync(dto.ClienteId);
            if (cliente == null)
            {
                throw new InvalidOperationException($"No se encontró el cliente con ID #{dto.ClienteId}.");
            }

            if (dto.PrestamoId.HasValue)
            {
                var prestamo = await _context.Prestamos.FindAsync(dto.PrestamoId.Value);
                if (prestamo == null || prestamo.ClienteId != dto.ClienteId)
                {
                    throw new InvalidOperationException("El préstamo seleccionado no existe o no pertenece al cliente especificado.");
                }
            }

            // Generar código correlativo GAR-0001
            var ultimoId = await _context.Garantias.OrderByDescending(g => g.Id).Select(g => g.Id).FirstOrDefaultAsync();
            var nuevoCodigo = $"GAR-{(ultimoId + 1):D4}";

            var garantia = new Garantia
            {
                Codigo = nuevoCodigo,
                ClienteId = dto.ClienteId,
                PrestamoId = dto.PrestamoId,
                Tipo = dto.Tipo.Trim(),
                Descripcion = dto.Descripcion.Trim(),
                NumeroSerie = string.IsNullOrWhiteSpace(dto.NumeroSerie) ? null : dto.NumeroSerie.Trim(),
                ValorEstimado = dto.ValorEstimado,
                EstadoCustodia = "En Custodia",
                UbicacionFisica = dto.UbicacionFisica.Trim(),
                FotoUrl = string.IsNullOrWhiteSpace(dto.FotoUrl) ? null : dto.FotoUrl.Trim(),
                Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones.Trim(),
                FechaIngreso = DateTime.UtcNow
            };

            _context.Garantias.Add(garantia);
            await _context.SaveChangesAsync();

            return (await GetGarantiaByIdAsync(garantia.Id))!;
        }

        public async Task<GarantiaResponseDto> ActualizarGarantiaAsync(int id, ActualizarGarantiaDto dto)
        {
            var garantia = await _context.Garantias.FindAsync(id);
            if (garantia == null)
            {
                throw new InvalidOperationException($"No se encontró la garantía con ID #{id}.");
            }

            if (dto.PrestamoId.HasValue)
            {
                var prestamo = await _context.Prestamos.FindAsync(dto.PrestamoId.Value);
                if (prestamo == null || prestamo.ClienteId != garantia.ClienteId)
                {
                    throw new InvalidOperationException("El préstamo seleccionado no existe o no pertenece al propietario de la garantía.");
                }
            }

            garantia.PrestamoId = dto.PrestamoId;
            garantia.Tipo = dto.Tipo.Trim();
            garantia.Descripcion = dto.Descripcion.Trim();
            garantia.NumeroSerie = string.IsNullOrWhiteSpace(dto.NumeroSerie) ? null : dto.NumeroSerie.Trim();
            garantia.ValorEstimado = dto.ValorEstimado;
            garantia.UbicacionFisica = dto.UbicacionFisica.Trim();
            if (dto.FotoUrl != null)
            {
                garantia.FotoUrl = string.IsNullOrWhiteSpace(dto.FotoUrl) ? null : dto.FotoUrl.Trim();
            }
            garantia.Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones.Trim();

            await _context.SaveChangesAsync();

            return (await GetGarantiaByIdAsync(garantia.Id))!;
        }

        public async Task<GarantiaResponseDto> CambiarEstadoAsync(int id, CambiarEstadoGarantiaDto dto)
        {
            var garantia = await _context.Garantias.FindAsync(id);
            if (garantia == null)
            {
                throw new InvalidOperationException($"No se encontró la garantía con ID #{id}.");
            }

            garantia.EstadoCustodia = dto.EstadoCustodia.Trim();

            if (dto.EstadoCustodia == "Devuelta")
            {
                garantia.FechaDevolucion = DateTime.UtcNow;
            }
            else if (dto.EstadoCustodia == "En Custodia")
            {
                garantia.FechaDevolucion = null;
            }

            if (!string.IsNullOrWhiteSpace(dto.Observaciones))
            {
                garantia.Observaciones = string.IsNullOrWhiteSpace(garantia.Observaciones)
                    ? $"[{DateTime.UtcNow:dd/MM/yyyy}] {dto.Observaciones.Trim()}"
                    : $"{garantia.Observaciones} | [{DateTime.UtcNow:dd/MM/yyyy}] {dto.Observaciones.Trim()}";
            }

            await _context.SaveChangesAsync();

            return (await GetGarantiaByIdAsync(garantia.Id))!;
        }

        public async Task<bool> EliminarGarantiaAsync(int id)
        {
            var garantia = await _context.Garantias.FindAsync(id);
            if (garantia == null) return false;

            _context.Garantias.Remove(garantia);
            await _context.SaveChangesAsync();
            return true;
        }

        private static GarantiaResponseDto MapToResponseDto(Garantia g)
        {
            decimal? saldoRestante = null;
            if (g.Prestamo != null)
            {
                var totalPagar = (g.Prestamo.CuotaMonto * g.Prestamo.PlazoCuotas);
                var totalPagado = g.Prestamo.Cuotas?.Sum(c => c.MontoPagadoPrincipal + c.MontoPagadoInteres) 
                    ?? (g.Prestamo.CuotaMonto * g.Prestamo.CuotasPagadas);
                saldoRestante = Math.Max(0, totalPagar - totalPagado);
            }

            return new GarantiaResponseDto
            {
                Id = g.Id,
                Codigo = g.Codigo,
                ClienteId = g.ClienteId,
                ClienteNombre = g.Cliente?.Nombre ?? "Desconocido",
                ClienteIdentidad = g.Cliente?.Identidad ?? "",
                ClientePhone = g.Cliente?.Phone ?? "",
                PrestamoId = g.PrestamoId,
                PrestamoCodigo = g.Prestamo != null ? $"PF-{g.Prestamo.Id:D4}" : null,
                PrestamoSaldoRestante = saldoRestante,
                Tipo = g.Tipo,
                Descripcion = g.Descripcion,
                NumeroSerie = g.NumeroSerie,
                ValorEstimado = g.ValorEstimado,
                EstadoCustodia = g.EstadoCustodia,
                UbicacionFisica = g.UbicacionFisica,
                FotoUrl = g.FotoUrl,
                Observaciones = g.Observaciones,
                FechaIngreso = g.FechaIngreso,
                FechaDevolucion = g.FechaDevolucion,
                FechaCreacion = g.FechaCreacion,
                CreadoPor = g.CreadoPor
            };
        }
    }
}
