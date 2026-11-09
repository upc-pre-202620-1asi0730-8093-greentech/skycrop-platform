using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace GreenTech.SkyCrop.Platform.Migrations
{
    /// <inheritdoc />
    public partial class AddDroneManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "drones",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    model = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    serial_number = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    serial_number_key = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false, collation: "utf8mb4_bin"),
                    camera = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    autonomy_minutes = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    battery = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    image_url = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: false),
                    version = table.Column<Guid>(type: "char(36)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_drones", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "flight_routes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false),
                    plot_id = table.Column<int>(type: "int", nullable: false),
                    altitude_meters = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    speed_meters_per_second = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    version = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_flight_routes", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "flight_route_waypoints",
                columns: table => new
                {
                    sequence = table.Column<int>(type: "int", nullable: false),
                    flight_route_id = table.Column<int>(type: "int", nullable: false),
                    latitude = table.Column<double>(type: "double", nullable: false),
                    longitude = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_flight_route_waypoints", x => new { x.flight_route_id, x.sequence });
                    table.ForeignKey(
                        name: "f_k_flight_route_waypoints_flight_routes_flight_route_id",
                        column: x => x.flight_route_id,
                        principalTable: "flight_routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "flights",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    drone_id = table.Column<int>(type: "int", nullable: false),
                    flight_route_id = table.Column<int>(type: "int", nullable: false),
                    plot_id = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    active_drone_id = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    started_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    duration_minutes = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    version = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_flights", x => x.id);
                    table.ForeignKey(
                        name: "f_k_flights_drones_drone_id",
                        column: x => x.drone_id,
                        principalTable: "drones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "f_k_flights_flight_routes_flight_route_id",
                        column: x => x.flight_route_id,
                        principalTable: "flight_routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "aerial_images",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    flight_id = table.Column<int>(type: "int", nullable: false),
                    file_url = table.Column<string>(type: "varchar(2048)", maxLength: 2048, nullable: false),
                    captured_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("p_k_aerial_images", x => x.id);
                    table.ForeignKey(
                        name: "f_k_aerial_images_flights_flight_id",
                        column: x => x.flight_id,
                        principalTable: "flights",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "i_x_aerial_images_flight_id",
                table: "aerial_images",
                column: "flight_id");

            migrationBuilder.CreateIndex(
                name: "ux_drones_serial_number_key",
                table: "drones",
                column: "serial_number_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "i_x_flights_drone_id",
                table: "flights",
                column: "drone_id");

            migrationBuilder.CreateIndex(
                name: "i_x_flights_flight_route_id",
                table: "flights",
                column: "flight_route_id");

            migrationBuilder.CreateIndex(
                name: "ux_flights_active_drone_id",
                table: "flights",
                column: "active_drone_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aerial_images");

            migrationBuilder.DropTable(
                name: "flight_route_waypoints");

            migrationBuilder.DropTable(
                name: "flights");

            migrationBuilder.DropTable(
                name: "drones");

            migrationBuilder.DropTable(
                name: "flight_routes");
        }
    }
}
