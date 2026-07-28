using PrestaFlow.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PrestaFlow.API.Data
{
    public class PrestaFlowDbContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PrestaFlowDbContext(DbContextOptions<PrestaFlowDbContext> options, IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        // Sets de Datos
        public DbSet<Usuario> Usuarios => Set<Usuario>();
        public DbSet<Cliente> Clientes => Set<Cliente>();
        public DbSet<Prestamo> Prestamos => Set<Prestamo>();
        public DbSet<Pago> Pagos => Set<Pago>();
        public DbSet<CuentaFinanciera> CuentasFinancieras => Set<CuentaFinanciera>();
        public DbSet<TransaccionFinanciera> TransaccionesFinancieras => Set<TransaccionFinanciera>();
        public DbSet<Cuota> Cuotas => Set<Cuota>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Convención PostgreSQL: Tablas en snake_case
            modelBuilder.Entity<Usuario>().ToTable("usuarios").HasIndex(u => u.Username).IsUnique();
            modelBuilder.Entity<Cliente>().ToTable("clientes").HasIndex(c => c.Identidad).IsUnique();
            modelBuilder.Entity<Prestamo>().ToTable("prestamos");
            modelBuilder.Entity<Pago>().ToTable("pagos");
            modelBuilder.Entity<CuentaFinanciera>().ToTable("cuentas_financieras");
            modelBuilder.Entity<TransaccionFinanciera>().ToTable("transacciones_financieras");
            modelBuilder.Entity<Cuota>().ToTable("cuotas");

            // Configurar relación 1-a-M (Un Cliente -> Muchos Préstamos)
            modelBuilder.Entity<Prestamo>()
                .HasOne(p => p.Cliente)
                .WithMany(c => c.Prestamos)
                .HasForeignKey(p => p.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configurar relación 1-a-M (Un Préstamo -> Muchos Pagos/Abonos)
            modelBuilder.Entity<Pago>()
                .HasOne(p => p.Prestamo)
                .WithMany(pr => pr.Pagos)
                .HasForeignKey(p => p.PrestamoId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configurar relación 1-a-M (Una Cuenta -> Muchas Transacciones)
            modelBuilder.Entity<TransaccionFinanciera>()
                .HasOne(t => t.Cuenta)
                .WithMany(c => c.Transacciones)
                .HasForeignKey(t => t.CuentaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configurar relación 1-a-M (Un Préstamo -> Muchas Cuotas)
            modelBuilder.Entity<Cuota>()
                .HasOne(c => c.Prestamo)
                .WithMany(p => p.Cuotas)
                .HasForeignKey(c => c.PrestamoId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        // Auditoría automática por interceptor en SaveChangesAsync
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var usuarioActual = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "sistema";
            var ahora = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.FechaCreacion = ahora;
                    entry.Entity.CreadoPor = usuarioActual;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.FechaModificacion = ahora;
                    entry.Entity.ModificadoPor = usuarioActual;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
