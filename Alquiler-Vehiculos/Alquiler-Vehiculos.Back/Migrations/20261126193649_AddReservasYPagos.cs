using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alquiler_Vehiculos.Migrations
{
    /// <inheritdoc />
    public partial class AddReservasYPagos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reservas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ClienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehiculoId = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    TrabajadorId = table.Column<Guid>(type: "uuid", nullable: true),
                    FechaRecogida = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaDevolucion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaDevolucionReal = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiraEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PrecioBaseDiaCongelado = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    NombrePolizaCongelada = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IncrementoPolizaDiaCongelado = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    PrecioTotal = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Divisa = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Observaciones = table.Column<string>(type: "text", nullable: true),
                    MotivoCancelacion = table.Column<string>(type: "text", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reservas_clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_reservas_trabajadores_TrabajadorId",
                        column: x => x.TrabajadorId,
                        principalTable: "trabajadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cambios_estado_reserva",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservaId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstadoAnterior = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    EstadoNuevo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RealizadoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    Motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaCambio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cambios_estado_reserva", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cambios_estado_reserva_reservas_ReservaId",
                        column: x => x.ReservaId,
                        principalTable: "reservas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cambios_estado_reserva_usuarios_RealizadoPorUsuarioId",
                        column: x => x.RealizadoPorUsuarioId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "lineas_reserva",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Concepto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    PolizaId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExtraId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lineas_reserva", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lineas_reserva_reservas_ReservaId",
                        column: x => x.ReservaId,
                        principalTable: "reservas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pagos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Importe = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ImporteReembolsado = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    Divisa = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MarcaTarjeta = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    UltimosCuatroDigitos = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    NumeroTarjeta = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ReferenciaPasarela = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MotivoRechazo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pagos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pagos_reservas_ReservaId",
                        column: x => x.ReservaId,
                        principalTable: "reservas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cambios_estado_reserva_RealizadoPorUsuarioId",
                table: "cambios_estado_reserva",
                column: "RealizadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_cambios_estado_reserva_ReservaId",
                table: "cambios_estado_reserva",
                column: "ReservaId");

            migrationBuilder.CreateIndex(
                name: "IX_lineas_reserva_ReservaId",
                table: "lineas_reserva",
                column: "ReservaId");

            migrationBuilder.CreateIndex(
                name: "UX_pagos_reserva_aprobado",
                table: "pagos",
                column: "ReservaId",
                unique: true,
                filter: "\"Estado\" = 'Aprobado'");

            migrationBuilder.CreateIndex(
                name: "IX_reservas_ClienteId",
                table: "reservas",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_reservas_Codigo",
                table: "reservas",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reservas_Estado_ExpiraEn",
                table: "reservas",
                columns: new[] { "Estado", "ExpiraEn" });

            migrationBuilder.CreateIndex(
                name: "IX_reservas_TrabajadorId",
                table: "reservas",
                column: "TrabajadorId");

            migrationBuilder.CreateIndex(
                name: "IX_reservas_VehiculoId_Estado",
                table: "reservas",
                columns: new[] { "VehiculoId", "Estado" });
            
            
            // Garantía declarativa de concurrencia en PostgreSQL (RF-30, RNF-01):
            // impide a nivel de motor que dos reservas vivas solapen el mismo vehículo.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            migrationBuilder.Sql("""
                                 ALTER TABLE reservas
                                 ADD CONSTRAINT "EX_reservas_vehiculo_solape"
                                 EXCLUDE USING gist (
                                     "VehiculoId" WITH =,
                                     tstzrange("FechaRecogida", "FechaDevolucion", '[)') WITH &&
                                 )
                                 WHERE ("Estado" IN ('Pendiente', 'Confirmada', 'EnCurso') AND NOT "IsDeleted");
                                 """);
        }
        
        

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                                 ALTER TABLE reservas
                                 DROP CONSTRAINT IF EXISTS "EX_reservas_vehiculo_solape";
                                 """);
            
            migrationBuilder.DropTable(
                name: "cambios_estado_reserva");

            migrationBuilder.DropTable(
                name: "lineas_reserva");

            migrationBuilder.DropTable(
                name: "pagos");

            migrationBuilder.DropTable(
                name: "reservas");
        }
    }
}
