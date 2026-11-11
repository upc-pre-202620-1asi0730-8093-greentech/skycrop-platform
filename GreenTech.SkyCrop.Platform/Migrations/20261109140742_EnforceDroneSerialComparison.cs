using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreenTech.SkyCrop.Platform.Migrations;

/// <summary>Enforces exact comparison of canonical serial keys with the Oracle MySQL provider.</summary>
public partial class EnforceDroneSerialComparison : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // This provider does not emit property-level collation in its generated column DDL.
        migrationBuilder.Sql("ALTER TABLE `drones` MODIFY `serial_number_key` varchar(120) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE `drones` MODIFY `serial_number_key` varchar(120) CHARACTER SET utf8mb4 NOT NULL;");
    }
}
