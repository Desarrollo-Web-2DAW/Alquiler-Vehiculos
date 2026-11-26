using Alquiler_Vehiculos.Models.Oficina;
using Alquiler_Vehiculos.Models.Pago;
using Alquiler_Vehiculos.Models.Reserva;
using Alquiler_Vehiculos.Models.Usuarios;
using Alquiler_Vehiculos.Models.Usuarios.Clientes;
using Alquiler_Vehiculos.Models.Usuarios.Trabajadores;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Alquiler_Vehiculos.Entities;

/// <summary>
/// Contexto único de la aplicación: Identity y dominio comparten base de datos.
/// Dos DbContext sobre la misma BD romperían la creación del esquema
/// (apuntes 16.6.2), así que todo lo que añada el equipo va aquí.
/// </summary>
public class AppDbContextUsuarios(DbContextOptions<AppDbContextUsuarios> options)
    : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options) {
    public DbSet<Cliente> Clientes { get; set; } = null!;
    public DbSet<Trabajador> Trabajadores { get; set; } = null!;
    public DbSet<Oficina> Oficinas { get; set; } = null!;
    public DbSet<Reserva> Reservas { get; set; } = null!;
    public DbSet<LineaReserva> LineasReserva { get; set; } = null!;
    public DbSet<CambioEstadoReserva> CambiosEstadoReserva { get; set; } = null!;
    public DbSet<Pago> Pagos { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        // ── Nombres de tabla ─────────────────────────────────────────────────
        // Identity en minúscula (apuntes 16.6.2) y el dominio con el mismo
        // criterio, para que el esquema no mezcle estilos.
        modelBuilder.Entity<User>().ToTable("usuarios");
        modelBuilder.Entity<IdentityRole<Guid>>().ToTable("roles");
        modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        modelBuilder.Entity<IdentityUserRole<Guid>>().ToTable("user_roles");
        modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");
        modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("role_claims");

        modelBuilder.Entity<Cliente>().ToTable("clientes");
        modelBuilder.Entity<Trabajador>().ToTable("trabajadores");

        // ── Índices únicos ───────────────────────────────────────────────────
        // La última garantía la da la base de datos: la comprobación en el
        // repositorio tiene condición de carrera y no impide duplicados.
        // En PostgreSQL un índice único admite varios NULL, así que los
        // documentos opcionales del cliente no dan problema.
        modelBuilder.Entity<Cliente>(entity => {
            entity.HasIndex(c => c.Email).IsUnique();
            entity.HasIndex(c => c.Dni).IsUnique();
            entity.HasIndex(c => c.Nie).IsUnique();
            entity.HasIndex(c => c.Pasaporte).IsUnique();
        });

        modelBuilder.Entity<Trabajador>(entity => {
            entity.HasIndex(t => t.Email).IsUnique();
            entity.HasIndex(t => t.Dni).IsUnique();
        });

        // Oficina única (RF-11): restricción CHECK sobre la clave fija + FK restrictiva al responsable (CU-18).
        modelBuilder.Entity<Oficina>(entity => {
            entity.ToTable("oficina", t => t.HasCheckConstraint(
                "CK_oficina_unica",
                $"\"Id\" = '{Oficina.OficinaUnicaId}'"));

            entity.HasOne(o => o.Responsable)
                .WithMany()
                .HasForeignKey(o => o.ResponsableId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Reservas (RF-25 a RF-33, RI-02, RNF-01) ──────────────────────────
        modelBuilder.Entity<Reserva>(entity => {
            entity.ToTable("reservas");
            entity.HasKey(r => r.Id);

            entity.Property(r => r.Codigo).IsRequired().HasMaxLength(32);
            entity.HasIndex(r => r.Codigo).IsUnique();

            entity.Property(r => r.VehiculoId).IsRequired().HasMaxLength(11);
            entity.Property(r => r.Estado).HasConversion<string>().HasMaxLength(20);
            entity.Property(r => r.Divisa).IsRequired().HasMaxLength(3);

            entity.Property(r => r.PrecioBaseDiaCongelado).HasPrecision(10, 2);
            entity.Property(r => r.NombrePolizaCongelada).IsRequired().HasMaxLength(100);
            entity.Property(r => r.IncrementoPolizaDiaCongelado).HasPrecision(10, 2);
            entity.Property(r => r.PrecioTotal).HasPrecision(10, 2);

            // Unidireccional (.WithMany() vacío): sin tocar Cliente ni Trabajador
            entity.HasOne(r => r.Cliente)
                .WithMany()
                .HasForeignKey(r => r.ClienteId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Trabajador)
                .WithMany()
                .HasForeignKey(r => r.TrabajadorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(r => new { r.VehiculoId, r.Estado });
            entity.HasIndex(r => new { r.Estado, r.ExpiraEn });
        });

        // ── Líneas congeladas de reserva (RF-25) ─────────────────────────────
        modelBuilder.Entity<LineaReserva>(entity => {
            entity.ToTable("lineas_reserva");
            entity.HasKey(l => l.Id);

            entity.Property(l => l.Tipo).HasConversion<string>().HasMaxLength(20);
            entity.Property(l => l.Concepto).IsRequired().HasMaxLength(200);
            entity.Property(l => l.PrecioUnitario).HasPrecision(10, 2);
            entity.Property(l => l.Subtotal).HasPrecision(10, 2);

            // Bidireccional: Reserva tiene ICollection<LineaReserva> Lineas
            entity.HasOne(l => l.Reserva)
                .WithMany(r => r.Lineas)
                .HasForeignKey(l => l.ReservaId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Historial de cambios de estado (RF-31, RI-05) ────────────────────
        modelBuilder.Entity<CambioEstadoReserva>(entity => {
            entity.ToTable("cambios_estado_reserva");
            entity.HasKey(c => c.Id);

            entity.Property(c => c.EstadoAnterior).HasConversion<string>().HasMaxLength(20);
            entity.Property(c => c.EstadoNuevo).HasConversion<string>().HasMaxLength(20);
            entity.Property(c => c.Motivo).HasMaxLength(500);

            // Bidireccional: Reserva tiene ICollection<CambioEstadoReserva> HistorialEstados
            entity.HasOne(c => c.Reserva)
                .WithMany(r => r.HistorialEstados)
                .HasForeignKey(c => c.ReservaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unidireccional (.WithMany() vacío): sin tocar User
            entity.HasOne(c => c.RealizadoPorUsuario)
                .WithMany()
                .HasForeignKey(c => c.RealizadoPorUsuarioId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Pagos (RF-21, RF-22, CU-20) ──────────────────────────────────────
        modelBuilder.Entity<Pago>(entity => {
            entity.ToTable("pagos");
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Importe).HasPrecision(10, 2);
            entity.Property(p => p.ImporteReembolsado).HasPrecision(10, 2);
            entity.Property(p => p.Divisa).IsRequired().HasMaxLength(3);
            entity.Property(p => p.Estado).HasConversion<string>().HasMaxLength(20);
            entity.Property(p => p.MarcaTarjeta).IsRequired().HasMaxLength(30);
            entity.Property(p => p.UltimosCuatroDigitos).IsRequired().HasMaxLength(4);
            entity.Property(p => p.NumeroTarjeta).IsRequired().HasMaxLength(512);
            entity.Property(p => p.ReferenciaPasarela).IsRequired().HasMaxLength(100);
            entity.Property(p => p.MotivoRechazo).HasMaxLength(500);

            // Bidireccional: Reserva tiene ICollection<Pago> Pagos (Restrict protege registros contables)
            entity.HasOne(p => p.Reserva)
                .WithMany(r => r.Pagos)
                .HasForeignKey(p => p.ReservaId)
                .OnDelete(DeleteBehavior.Restrict);

            // CU-20: como máximo un pago Aprobado por reserva
            entity.HasIndex(p => p.ReservaId)
                .IsUnique()
                .HasDatabaseName("UX_pagos_reserva_aprobado")
                .HasFilter("\"Estado\" = 'Aprobado'");
        });
    }
}