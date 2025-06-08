using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MigrationsProject.Migrations
{
    /// <inheritdoc />
    public partial class NewUpdate1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "goals",
                table: "users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "goals",
                table: "users");
        }
    }
}
