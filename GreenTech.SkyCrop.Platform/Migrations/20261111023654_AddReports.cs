using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace GreenTech.SkyCrop.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reports",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    title = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    generated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    season = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    year = table.Column<int>(type: "int", nullable: false),
                    owner = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    generated_by = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    flights = table.Column<int>(type: "int", nullable: false),
                    alerts = table.Column<int>(type: "int", nullable: false),
                    average_ndvi = table.Column<float>(type: "float", nullable: false),
                    area_hectares = table.Column<float>(type: "float", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_reports", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ndvi_evolution_items",
                columns: table => new
                {
                    date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    report_id = table.Column<int>(type: "int", nullable: false),
                    value = table.Column<float>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_ndvi_evolution_items", x => new { x.report_id, x.date });
                    table.ForeignKey(
                        name: "f_k_ndvi_evolution_items_reports_report_id",
                        column: x => x.report_id,
                        principalTable: "reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "report_performance_items",
                columns: table => new
                {
                    plot_id = table.Column<int>(type: "int", nullable: false),
                    report_id = table.Column<int>(type: "int", nullable: false),
                    alerts = table.Column<int>(type: "int", nullable: false),
                    flights = table.Column<int>(type: "int", nullable: false),
                    average_ndvi = table.Column<float>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_report_performance_items", x => new { x.report_id, x.plot_id });
                    table.ForeignKey(
                        name: "f_k_report_performance_items_reports_report_id",
                        column: x => x.report_id,
                        principalTable: "reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ndvi_evolution_items");

            migrationBuilder.DropTable(
                name: "report_performance_items");

            migrationBuilder.DropTable(
                name: "reports");
        }
    }
}
