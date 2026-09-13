using PrestaFlow.API.Data;
using PrestaFlow.API.Entities;
using PrestaFlow.API.Features.Clientes.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PrestaFlow.API.Features.Clientes
{
    public class ClientesService
    {
        private readonly PrestaFlowDbContext _context;

        public ClientesService(PrestaFlowDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene todos los clientes registrados con sus indicadores financieros acumulados.
        /// </summary>
        public async Task<List<ClienteResponseDto>> GetClientesAsync()
        {
            var clientes = await _context.Clientes
                .Include(c => c.Prestamos)
                    .ThenInclude(p => p.Pagos)
                .Include(c => c.Prestamos)
                    .ThenInclude(p => p.Cuotas)
                .OrderBy(c => c.Nombre)
                .ToListAsync();

            var dtos = clientes.Select(MapToResponseDto).ToList();
            await _context.SaveChangesAsync();
            return dtos;
        }

        /// <summary>
        /// Obtiene un cliente por su ID único.
        /// </summary>
        public async Task<ClienteResponseDto?> GetClienteByIdAsync(int id)
        {
            var cliente = await _context.Clientes
                .Include(c => c.Prestamos)
                    .ThenInclude(p => p.Pagos)
                .Include(c => c.Prestamos)
                    .ThenInclude(p => p.Cuotas)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cliente == null) return null;

            var dto = MapToResponseDto(cliente);
            await _context.SaveChangesAsync();
            return dto;
        }

        /// <summary>
        /// Crea un nuevo perfil de cliente en la base de datos.
        /// </summary>
        public async Task<ClienteResponseDto> CreateClienteAsync(ClienteCreateDto dto)
        {
            // Validar si ya existe la identidad
            var existe = await _context.Clientes.AnyAsync(c => c.Identidad == dto.Identidad);
            if (existe)
            {
                throw new InvalidOperationException($"Ya existe un cliente registrado con la identidad '{dto.Identidad}'.");
            }

            var cliente = new Cliente
            {
                Identidad = dto.Identidad,
                Nombre = dto.Nombre,
                Phone = dto.Phone,
                Address = dto.Address,
                Zone = dto.Zone,
                RefName = dto.RefName,
                RefPhone = dto.RefPhone
            };

            await _context.Clientes.AddAsync(cliente);
            await _context.SaveChangesAsync();

            return MapToResponseDto(cliente);
        }

        /// <summary>
        /// Mapea una entidad Cliente a su DTO de respuesta y calcula los KPIs crediticios.
        /// </summary>
        private ClienteResponseDto MapToResponseDto(Cliente c)
        {
            // Sincronizar estado de cada préstamo en tiempo real
            foreach (var p in c.Prestamos)
            {
                if (p.Cuotas != null && p.Cuotas.Any())
                {
                    PrestaFlow.API.Features.Pagos.PagosService.ActualizarMoraYRecalculos(p);
                }
                else
                {
                    decimal totalConInteres = p.Capital * (1 + (p.InteresPorcentaje / 100m));
                    decimal totalAbonado = p.Pagos?.Sum(pg => pg.Monto) ?? 0m;
                    if (totalConInteres - totalAbonado <= 0.05m)
                    {
                        p.Status = "Pagado";
                        p.CuotasPagadas = p.PlazoCuotas;
                    }
                }
            }

            var activeLoans = c.Prestamos.Where(p => p.Status != "Pagado").ToList();
            var loansCount = activeLoans.Count;

            // Calcular saldo deudor pendiente
            decimal balance = 0;
            foreach (var prestamo in c.Prestamos)
            {
                if (prestamo.Status == "Pagado") continue;

                // Total a pagar con interés
                decimal totalConInteres = prestamo.Capital * (1 + (prestamo.InteresPorcentaje / 100m));
                
                // Total abonado hasta la fecha
                decimal totalAbonado = prestamo.Pagos?.Sum(p => p.Monto) ?? (prestamo.Cuotas?.Sum(cu => cu.MontoPagadoPrincipal + cu.MontoPagadoInteres + cu.MontoPagadoMora) ?? 0m);
                
                decimal restante = totalConInteres - totalAbonado;
                if (restante > 0.05m)
                {
                    balance += restante;
                }
            }

            // Calcular estado financiero general
            string status = "Sin Crédito";
            if (c.Prestamos.Count == 0)
            {
                status = "Sin Crédito";
            }
            else if (c.Prestamos.Any(p => p.Status == "Mora"))
            {
                status = "En Mora";
            }
            else if (c.Prestamos.Any(p => p.Status == "Activo"))
            {
                status = "Al Día";
            }
            else if (c.Prestamos.All(p => p.Status == "Pagado"))
            {
                status = "Al Día";
            }

            // Calcular score comportamiento de pago
            string score = "Nuevo";
            if (c.Prestamos.Count > 0)
            {
                if (c.Prestamos.Any(p => p.Status == "Mora"))
                {
                    score = "Mora";
                }
                else if (c.Prestamos.All(p => p.Status == "Pagado"))
                {
                    score = "Excelente";
                }
                else
                {
                    score = "Regular";
                }
            }

            // Mapear historial de préstamos individuales
            var history = c.Prestamos.Select(p => new ClienteLoanHistoryDto
            {
                LoanId = $"PF-{p.Id:D4}", // Genera códigos tipo PF-1001
                Amount = p.Capital,
                Interest = p.InteresPorcentaje,
                Date = p.FechaOtorgado,
                Status = p.Status,
                Cuotas = $"{p.CuotasPagadas}/{p.PlazoCuotas}"
            })
            .OrderByDescending(h => h.Date)
            .ToList();

            return new ClienteResponseDto
            {
                Id = c.Id,
                Identidad = c.Identidad,
                Nombre = c.Nombre,
                Phone = c.Phone,
                Address = c.Address,
                Zone = c.Zone,
                RefName = c.RefName,
                RefPhone = c.RefPhone,
                LoansCount = loansCount,
                Balance = balance,
                Status = status,
                Score = score,
                PrestamosHistory = history
            };
        }
    }
}
