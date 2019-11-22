using Microsoft.EntityFrameworkCore.Migrations;

namespace Micser.Engine.Infrastructure.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                "Modules",
                table => new
                {
                    Id = table.Column<long>()
                        .Annotation("Sqlite:Autoincrement", true),
                    ModuleType = table.Column<string>(nullable: true),
                    StateJson = table.Column<string>(nullable: true),
                    WidgetType = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                "Settings",
                table => new
                {
                    Id = table.Column<long>()
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(nullable: true),
                    ValueJson = table.Column<string>(nullable: true),
                    ValueType = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                "ModuleConnections",
                table => new
                {
                    Id = table.Column<long>()
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceConnectorName = table.Column<string>(nullable: true),
                    SourceModuleId = table.Column<long>(),
                    TargetConnectorName = table.Column<string>(nullable: true),
                    TargetModuleId = table.Column<long>()
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleConnections", x => x.Id);
                    table.ForeignKey(
                        "FK_ModuleConnections_Modules_SourceModuleId",
                        x => x.SourceModuleId,
                        "Modules",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        "FK_ModuleConnections_Modules_TargetModuleId",
                        x => x.TargetModuleId,
                        "Modules",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                "IX_ModuleConnections_SourceModuleId",
                "ModuleConnections",
                "SourceModuleId");

            migrationBuilder.CreateIndex(
                "IX_ModuleConnections_TargetModuleId",
                "ModuleConnections",
                "TargetModuleId");

            migrationBuilder.CreateIndex(
                "IX_Settings_Key",
                "Settings",
                "Key",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "ModuleConnections");

            migrationBuilder.DropTable(
                "Settings");

            migrationBuilder.DropTable(
                "Modules");
        }
    }
}
