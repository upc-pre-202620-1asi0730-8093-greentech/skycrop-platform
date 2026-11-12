using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace GreenTech.SkyCrop.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddDiagnoses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "diagnoses",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    plot_id = table.Column<int>(type: "int", nullable: false),
                    flight_id = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    scenario = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    source = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    monitored_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    requested_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    generated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    observations = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    average_ndvi = table.Column<double>(type: "double", nullable: true),
                    failure_reason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    image_ids = table.Column<string>(type: "longtext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_diagnoses", x => x.id);
                    table.ForeignKey(
                        name: "f_k_diagnoses_agricultural_plots_plot_id",
                        column: x => x.plot_id,
                        principalTable: "agricultural_plots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_diagnoses_flights_flight_id",
                        column: x => x.flight_id,
                        principalTable: "flights",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "diagnosis_anomalies",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    diagnosis_id = table.Column<int>(type: "int", nullable: false),
                    type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    severity = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    location = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    latitude = table.Column<double>(type: "double", nullable: true),
                    longitude = table.Column<double>(type: "double", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_diagnosis_anomalies", x => x.id);
                    table.ForeignKey(
                        name: "f_k_diagnosis_anomalies_diagnoses_diagnosis_id",
                        column: x => x.diagnosis_id,
                        principalTable: "diagnoses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "i_x_diagnoses_flight_id",
                table: "diagnoses",
                column: "flight_id");

            migrationBuilder.CreateIndex(
                name: "i_x_diagnoses_plot_id_monitored_at_id",
                table: "diagnoses",
                columns: new[] { "plot_id", "monitored_at", "id" });

            migrationBuilder.CreateIndex(
                name: "i_x_diagnosis_anomalies_diagnosis_id",
                table: "diagnosis_anomalies",
                column: "diagnosis_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "diagnosis_anomalies");

            migrationBuilder.DropTable(
                name: "diagnoses");
        }
    }
}
