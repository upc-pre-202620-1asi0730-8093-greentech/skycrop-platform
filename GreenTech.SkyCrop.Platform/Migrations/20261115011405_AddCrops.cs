using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace GreenTech.SkyCrop.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddCrops : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "crops",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    variety = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    planting_date = table.Column<DateTime>(type: "date", nullable: false),
                    expected_harvest = table.Column<DateTime>(type: "date", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    planted_area_hectares = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    sowing_method = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    irrigation_type = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    soil_type = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    notes = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    image_u_rl = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: false),
                    growth_stage = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    ideal_humidity_min = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ideal_humidity_max = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_crops", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "crops");
        }
    }
}
