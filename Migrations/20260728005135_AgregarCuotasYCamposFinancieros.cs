using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PrestaFlow.API.Migrations
{
    /// <inheritdoc />
    public partial class AgregarCuotasYCamposFinancieros : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MetodoDesembolso",
                table: "prestamos",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TasaMoraPorcentaje",
                table: "prestamos",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TipoInteres",
                table: "prestamos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TipoPrestamo",
                table: "prestamos",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "MontoInteres",
                table: "pagos",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoMora",
                table: "pagos",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoPrincipal",
                table: "pagos",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "cuotas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PrestamoId = table.Column<int>(type: "integer", nullable: false),
                    NumeroCuota = table.Column<int>(type: "integer", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MontoPrincipal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MontoInteres = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MontoMoratorio = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MontoPagadoPrincipal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MontoPagadoInteres = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MontoPagadoMora = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaUltimoCalculoMora = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreadoPor = table.Column<string>(type: "text", nullable: false),
                    FechaModificacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ModificadoPor = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cuotas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cuotas_prestamos_PrestamoId",
                        column: x => x.PrestamoId,
                        principalTable: "prestamos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cuotas_PrestamoId",
                table: "cuotas",
                column: "PrestamoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cuotas");

            migrationBuilder.DropColumn(
                name: "MetodoDesembolso",
                table: "prestamos");

            migrationBuilder.DropColumn(
                name: "TasaMoraPorcentaje",
                table: "prestamos");

            migrationBuilder.DropColumn(
                name: "TipoInteres",
                table: "prestamos");

            migrationBuilder.DropColumn(
                name: "TipoPrestamo",
                table: "prestamos");

            migrationBuilder.DropColumn(
                name: "MontoInteres",
                table: "pagos");

            migrationBuilder.DropColumn(
                name: "MontoMora",
                table: "pagos");

            migrationBuilder.DropColumn(
                name: "MontoPrincipal",
                table: "pagos");
        }
    }
}
