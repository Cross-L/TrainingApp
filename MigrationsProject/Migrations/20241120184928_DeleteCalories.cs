using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MigrationsProject.Migrations
{
    /// <inheritdoc />
    public partial class DeleteCalories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "calories_burned",
                table: "exercises");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "calories_burned",
                table: "exercises",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);
        }
    }
}
