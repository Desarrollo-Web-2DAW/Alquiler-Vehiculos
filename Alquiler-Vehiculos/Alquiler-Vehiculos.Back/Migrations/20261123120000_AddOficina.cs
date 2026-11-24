using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Alquiler_Vehiculos.Migrations
{
    /// <inheritdoc />
    public partial class AddOficina : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "oficina",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "text", nullable: false),
                    Direccion = table.Column<string>(type: "text", nullable: false),
                    Ciudad = table.Column<string>(type: "text", nullable: false),
                    CodigoPostal = table.Column<string>(type: "text", nullable: false),
                    Telefono = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Latitud = table.Column<double>(type: "double precision", nullable: false),
                    Longitud = table.Column<double>(type: "double precision", nullable: false),
                    DiaInicio = table.Column<int>(type: "integer", nullable: false),
                    DiaFin = table.Column<int>(type: "integer", nullable: false),
                    HoraApertura = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    HoraCierre = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    ResponsableId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_oficina", x => x.Id);
                    table.CheckConstraint("CK_oficina_unica", "\"Id\" = '11111111-1111-1111-1111-111111111111'");
                    table.ForeignKey(
                        name: "FK_oficina_trabajadores_ResponsableId",
                        column: x => x.ResponsableId,
                        principalTable: "trabajadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_oficina_ResponsableId",
                table: "oficina",
                column: "ResponsableId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "oficina");
        }
    }
}
