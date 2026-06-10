using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpGoDown.Api.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Login = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Student"),
                PasswordHash = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_Users", x => x.Id));

        migrationBuilder.CreateTable(
            name: "LevelAttempts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                LevelId = table.Column<int>(type: "integer", nullable: false),
                Success = table.Column<bool>(type: "boolean", nullable: false),
                StepsCount = table.Column<int>(type: "integer", nullable: false),
                PointsHistoryJson = table.Column<string>(type: "text", nullable: false),
                SceneJson = table.Column<string>(type: "text", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LevelAttempts", x => x.Id);
                table.ForeignKey(
                    name: "FK_LevelAttempts_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_LevelAttempts_UserId_LevelId_CreatedAt",
            table: "LevelAttempts",
            columns: new[] { "UserId", "LevelId", "CreatedAt" });

        migrationBuilder.CreateIndex(
            name: "IX_Users_Login",
            table: "Users",
            column: "Login",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "LevelAttempts");
        migrationBuilder.DropTable(name: "Users");
    }
}
