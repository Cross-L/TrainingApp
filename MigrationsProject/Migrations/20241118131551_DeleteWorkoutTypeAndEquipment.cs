using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MigrationsProject.Migrations
{
    /// <inheritdoc />
    public partial class DeleteWorkoutTypeAndEquipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "equipment_needed",
                table: "workouts");

            migrationBuilder.DropColumn(
                name: "workout_type",
                table: "workouts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "equipment_needed",
                table: "workouts",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "workout_type",
                table: "workouts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
