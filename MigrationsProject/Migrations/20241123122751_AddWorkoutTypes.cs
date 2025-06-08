using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MigrationsProject.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkoutTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "goal",
                table: "workouts");

            migrationBuilder.AddColumn<string>(
                name: "goals",
                table: "workouts",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "workout_types",
                table: "workouts",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "goals",
                table: "workouts");

            migrationBuilder.DropColumn(
                name: "workout_types",
                table: "workouts");

            migrationBuilder.AddColumn<string>(
                name: "goal",
                table: "workouts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
