using Microsoft.EntityFrameworkCore.Migrations;

namespace Micser.Engine.Infrastructure.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Modules",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
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
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
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
                name: "ModuleConnections",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceConnectorName = table.Column<string>(nullable: true),
                    SourceModuleId = table.Column<long>(nullable: false),
                    TargetConnectorName = table.Column<string>(nullable: true),
                    TargetModuleId = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModuleConnections_Modules_SourceModuleId",
                        column: x => x.SourceModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ModuleConnections_Modules_TargetModuleId",
                        column: x => x.TargetModuleId,
                        principalTable: "Modules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleConnections_SourceModuleId",
                table: "ModuleConnections",
                column: "SourceModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuleConnections_TargetModuleId",
                table: "ModuleConnections",
                column: "TargetModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_Settings_Key",
                table: "Settings",
                column: "Key",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModuleConnections");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "Modules");
        }
    }
}
