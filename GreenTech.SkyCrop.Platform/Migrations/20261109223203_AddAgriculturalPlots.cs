using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace GreenTech.SkyCrop.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddAgriculturalPlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agricultural_plots",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    person_in_charge = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    crop_type = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    soil_type = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    sowing_date = table.Column<DateOnly>(type: "date", nullable: false),
                    area_hectares = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    location = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    latitude = table.Column<double>(type: "double", nullable: true),
                    longitude = table.Column<double>(type: "double", nullable: true),
                    image_url = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: false),
                    status = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_agricultural_plots", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agricultural_plots");
        }
    }
}
