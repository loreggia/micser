using Microsoft.EntityFrameworkCore.Migrations;

namespace Micser.App.Infrastructure.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                "IX_Settings_Key",
                "Settings",
                "Key",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                "Settings");
        }
    }
}
