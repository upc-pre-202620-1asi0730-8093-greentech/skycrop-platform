using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreenTech.SkyCrop.Platform.Migrations
{
    /// <inheritdoc />
    public partial class MakeDiagnosesStandalone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_diagnoses_agricultural_plots_plot_id",
                table: "diagnoses");

            migrationBuilder.DropForeignKey(
                name: "f_k_diagnoses_flights_flight_id",
                table: "diagnoses");

            migrationBuilder.DropIndex(
                name: "i_x_diagnoses_flight_id",
                table: "diagnoses");

            migrationBuilder.AlterColumn<int>(
                name: "plot_id",
                table: "diagnoses",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "flight_id",
                table: "diagnoses",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "plot_id",
                table: "diagnoses",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "flight_id",
                table: "diagnoses",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "i_x_diagnoses_flight_id",
                table: "diagnoses",
                column: "flight_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_diagnoses_agricultural_plots_plot_id",
                table: "diagnoses",
                column: "plot_id",
                principalTable: "agricultural_plots",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_diagnoses_flights_flight_id",
                table: "diagnoses",
                column: "flight_id",
                principalTable: "flights",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
