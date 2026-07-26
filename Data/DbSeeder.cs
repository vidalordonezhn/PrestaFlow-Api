using PrestaFlow.API.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using BCrypt.Net;

namespace PrestaFlow.API.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(PrestaFlowDbContext context)
        {
            // Asegurar que la base de datos existe
            await context.Database.EnsureCreatedAsync();

            // Si no hay usuarios, sembrar el administrador inicial
            if (!await context.Usuarios.AnyAsync())
            {
                var adminUser = new Usuario
                {
                    Username = "admin",
                    Nombre = "Administrador Principal",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123*"),
                    Rol = "Admin",
                    Activo = true
                };

                await context.Usuarios.AddAsync(adminUser);
                await context.SaveChangesAsync();
            }

            // Si no hay cuentas financieras, sembrar Caja y Banco iniciales
            if (!await context.CuentasFinancieras.AnyAsync())
            {
                var cuentas = new List<CuentaFinanciera>
                {
                    new CuentaFinanciera { Nombre = "Caja Chica General", Tipo = "Caja", Saldo = 50000.00m },
                    new CuentaFinanciera { Nombre = "Banco Atlántida - Cta. Principal", Tipo = "Banco", Saldo = 150000.00m }
                };

                await context.CuentasFinancieras.AddRangeAsync(cuentas);
                await context.SaveChangesAsync();
            }
        }
    }
}
