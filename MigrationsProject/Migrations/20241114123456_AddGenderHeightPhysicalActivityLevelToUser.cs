using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MigrationsProject.Migrations
{
    /// <inheritdoc />
    public partial class AddGenderHeightPhysicalActivityLevelToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "gender",
                table: "users",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "height",
                table: "users",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "physical_activity_level",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "gender",
                table: "users");

            migrationBuilder.DropColumn(
                name: "height",
                table: "users");

            migrationBuilder.DropColumn(
                name: "physical_activity_level",
                table: "users");
        }
    }
}
