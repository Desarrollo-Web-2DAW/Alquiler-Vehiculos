using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Alquiler_Vehiculos.Migrations.Catalogo
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Concesionarios",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Ciudad = table.Column<string>(type: "text", nullable: false),
                    Telefono = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Concesionarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Fotos",
                columns: table => new
                {
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Ruta = table.Column<string>(type: "text", nullable: false),
                    Extension = table.Column<string>(type: "text", nullable: false),
                    Tamano = table.Column<long>(type: "bigint", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fotos", x => x.Nombre);
                });

            migrationBuilder.CreateTable(
                name: "Vehiculos",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FotoFk = table.Column<string>(type: "text", nullable: false),
                    Matrícula = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    Marca = table.Column<string>(type: "text", nullable: false),
                    Modelo = table.Column<string>(type: "text", nullable: false),
                    Acriss = table.Column<string>(type: "text", nullable: false),
                    Carroceria = table.Column<int>(type: "integer", nullable: false),
                    Combustible = table.Column<int>(type: "integer", nullable: false),
                    Transmision = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<int>(type: "integer", nullable: false),
                    Color = table.Column<int>(type: "integer", nullable: false),
                    PrecioBaseDia = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Plazas = table.Column<int>(type: "integer", nullable: false),
                    MaleteroLitros = table.Column<int>(type: "integer", nullable: false),
                    AutonomiaKm = table.Column<int>(type: "integer", nullable: false),
                    Kilometraje = table.Column<int>(type: "integer", nullable: false),
                    FechaUltimaRevision = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConesionarioFk = table.Column<long>(type: "bigint", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehiculos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vehiculos_Concesionarios_ConesionarioFk",
                        column: x => x.ConesionarioFk,
                        principalTable: "Concesionarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vehiculos_Fotos_FotoFk",
                        column: x => x.FotoFk,
                        principalTable: "Fotos",
                        principalColumn: "Nombre",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Concesionarios_Telefono",
                table: "Concesionarios",
                column: "Telefono",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fotos_Ruta",
                table: "Fotos",
                column: "Ruta",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculos_Carroceria",
                table: "Vehiculos",
                column: "Carroceria");

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculos_Combustible",
                table: "Vehiculos",
                column: "Combustible");

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculos_ConesionarioFk",
                table: "Vehiculos",
                column: "ConesionarioFk");

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculos_Estado",
                table: "Vehiculos",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculos_FotoFk",
                table: "Vehiculos",
                column: "FotoFk",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculos_Marca",
                table: "Vehiculos",
                column: "Marca");

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculos_Matrícula",
                table: "Vehiculos",
                column: "Matrícula",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehiculos_Modelo",
                table: "Vehiculos",
                column: "Modelo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Vehiculos");

            migrationBuilder.DropTable(
                name: "Concesionarios");

            migrationBuilder.DropTable(
                name: "Fotos");
        }
    }
}
