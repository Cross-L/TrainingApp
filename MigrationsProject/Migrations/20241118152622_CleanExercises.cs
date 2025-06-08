using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MigrationsProject.Migrations
{
    /// <inheritdoc />
    public partial class CleanExercises : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "benefits",
                table: "exercises");

            migrationBuilder.DropColumn(
                name: "contraindications",
                table: "exercises");

            migrationBuilder.DropColumn(
                name: "modifications",
                table: "exercises");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "benefits",
                table: "exercises",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contraindications",
                table: "exercises",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "modifications",
                table: "exercises",
                type: "text",
                nullable: true);
        }
    }
}
