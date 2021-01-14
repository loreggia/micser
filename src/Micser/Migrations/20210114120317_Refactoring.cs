using Microsoft.EntityFrameworkCore.Migrations;

namespace Micser.Migrations
{
    public partial class Refactoring : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ModuleType",
                table: "Modules",
                newName: "Type");

            migrationBuilder.DropColumn(
                name: "WidgetType",
                table: "Modules");

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "Settings",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "StateJson",
                table: "Modules",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TargetConnectorName",
                table: "ModuleConnections",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SourceConnectorName",
                table: "ModuleConnections",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Modules",
                newName: "ModuleType");

            migrationBuilder.AlterColumn<string>(
                name: "Key",
                table: "Settings",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "StateJson",
                table: "Modules",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "WidgetType",
                table: "Modules",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TargetConnectorName",
                table: "ModuleConnections",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "SourceConnectorName",
                table: "ModuleConnections",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }
    }
}
