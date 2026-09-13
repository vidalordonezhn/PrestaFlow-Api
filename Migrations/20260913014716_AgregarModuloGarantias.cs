using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PrestaFlow.API.Migrations
{
    /// <inheritdoc />
    public partial class AgregarModuloGarantias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsAnulado",
                table: "pagos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAnulacion",
                table: "pagos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoAnulacion",
                table: "pagos",
                type: "character varying(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "garantias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ClienteId = table.Column<int>(type: "integer", nullable: false),
                    PrestamoId = table.Column<int>(type: "integer", nullable: true),
                    Tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    NumeroSerie = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ValorEstimado = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    EstadoCustodia = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UbicacionFisica = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    FotoUrl = table.Column<string>(type: "text", nullable: true),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaIngreso = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaDevolucion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreadoPor = table.Column<string>(type: "text", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModificadoPor = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_garantias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_garantias_clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_garantias_prestamos_PrestamoId",
                        column: x => x.PrestamoId,
                        principalTable: "prestamos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_garantias_ClienteId",
                table: "garantias",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_garantias_PrestamoId",
                table: "garantias",
                column: "PrestamoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "garantias");

            migrationBuilder.DropColumn(
                name: "EsAnulado",
                table: "pagos");

            migrationBuilder.DropColumn(
                name: "FechaAnulacion",
                table: "pagos");

            migrationBuilder.DropColumn(
                name: "MotivoAnulacion",
                table: "pagos");
        }
    }
}
