using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MigrationsProject.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    tag_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("tags_pkey", x => x.tag_id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    age = table.Column<int>(type: "integer", nullable: true),
                    weight = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    preferred_training_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    registered_via = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    registration_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    google_user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("users_pkey", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "exercises",
                columns: table => new
                {
                    exercise_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    difficulty_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    exercise_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    equipment_needed = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    duration = table.Column<int>(type: "integer", nullable: true),
                    reps = table.Column<int>(type: "integer", nullable: true),
                    sets = table.Column<int>(type: "integer", nullable: true),
                    calories_burned = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    precautions = table.Column<string>(type: "text", nullable: true),
                    benefits = table.Column<string>(type: "text", nullable: true),
                    contraindications = table.Column<string>(type: "text", nullable: true),
                    tags = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    modifications = table.Column<string>(type: "text", nullable: true),
                    author_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    average_rating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true, defaultValueSql: "0"),
                    popularity = table.Column<int>(type: "integer", nullable: true, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("exercises_pkey", x => x.exercise_id);
                    table.ForeignKey(
                        name: "exercises_author_id_fkey",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "workouts",
                columns: table => new
                {
                    workout_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    duration = table.Column<int>(type: "integer", nullable: true),
                    intensity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    goal = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    equipment_needed = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    workout_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    schedule = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    tags = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    author_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    average_rating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true, defaultValueSql: "0"),
                    popularity = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    calories_burned = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("workouts_pkey", x => x.workout_id);
                    table.ForeignKey(
                        name: "workouts_author_id_fkey",
                        column: x => x.author_id,
                        principalTable: "users",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "exercise_media",
                columns: table => new
                {
                    media_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    exercise_id = table.Column<int>(type: "integer", nullable: true),
                    media_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    media_url = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    uploaded_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("exercise_media_pkey", x => x.media_id);
                    table.ForeignKey(
                        name: "exercise_media_exercise_id_fkey",
                        column: x => x.exercise_id,
                        principalTable: "exercises",
                        principalColumn: "exercise_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exercise_muscles",
                columns: table => new
                {
                    exercise_id = table.Column<int>(type: "integer", nullable: false),
                    muscle_group = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("exercise_muscles_pkey", x => new { x.exercise_id, x.muscle_group });
                    table.ForeignKey(
                        name: "exercise_muscles_exercise_id_fkey",
                        column: x => x.exercise_id,
                        principalTable: "exercises",
                        principalColumn: "exercise_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exercise_reviews",
                columns: table => new
                {
                    review_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    exercise_id = table.Column<int>(type: "integer", nullable: true),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: true),
                    comment = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("exercise_reviews_pkey", x => x.review_id);
                    table.ForeignKey(
                        name: "exercise_reviews_exercise_id_fkey",
                        column: x => x.exercise_id,
                        principalTable: "exercises",
                        principalColumn: "exercise_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "exercise_reviews_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "exercise_tags",
                columns: table => new
                {
                    exercise_id = table.Column<int>(type: "integer", nullable: false),
                    tag_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("exercise_tags_pkey", x => new { x.exercise_id, x.tag_id });
                    table.ForeignKey(
                        name: "exercise_tags_exercise_id_fkey",
                        column: x => x.exercise_id,
                        principalTable: "exercises",
                        principalColumn: "exercise_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "exercise_tags_tag_id_fkey",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "tag_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_workouts",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    workout_id = table.Column<int>(type: "integer", nullable: false),
                    added_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("user_workouts_pkey", x => new { x.user_id, x.workout_id });
                    table.ForeignKey(
                        name: "user_workouts_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "user_workouts_workout_id_fkey",
                        column: x => x.workout_id,
                        principalTable: "workouts",
                        principalColumn: "workout_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_exercises",
                columns: table => new
                {
                    workout_id = table.Column<int>(type: "integer", nullable: false),
                    exercise_id = table.Column<int>(type: "integer", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: true),
                    reps = table.Column<int>(type: "integer", nullable: true),
                    sets = table.Column<int>(type: "integer", nullable: true),
                    rest_time = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("workout_exercises_pkey", x => new { x.workout_id, x.exercise_id });
                    table.ForeignKey(
                        name: "workout_exercises_exercise_id_fkey",
                        column: x => x.exercise_id,
                        principalTable: "exercises",
                        principalColumn: "exercise_id");
                    table.ForeignKey(
                        name: "workout_exercises_workout_id_fkey",
                        column: x => x.workout_id,
                        principalTable: "workouts",
                        principalColumn: "workout_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_reviews",
                columns: table => new
                {
                    review_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    workout_id = table.Column<int>(type: "integer", nullable: true),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: true),
                    comment = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp without time zone", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("workout_reviews_pkey", x => x.review_id);
                    table.ForeignKey(
                        name: "workout_reviews_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "workout_reviews_workout_id_fkey",
                        column: x => x.workout_id,
                        principalTable: "workouts",
                        principalColumn: "workout_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workout_tags",
                columns: table => new
                {
                    workout_id = table.Column<int>(type: "integer", nullable: false),
                    tag_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("workout_tags_pkey", x => new { x.workout_id, x.tag_id });
                    table.ForeignKey(
                        name: "workout_tags_tag_id_fkey",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "tag_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "workout_tags_workout_id_fkey",
                        column: x => x.workout_id,
                        principalTable: "workouts",
                        principalColumn: "workout_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_exercise_media_exercise_id",
                table: "exercise_media",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_exercise_reviews_exercise_id",
                table: "exercise_reviews",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_exercise_reviews_user_id",
                table: "exercise_reviews",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_exercise_tags_tag_id",
                table: "exercise_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_exercises_author_id",
                table: "exercises",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "tags_name_key",
                table: "tags",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_workouts_workout_id",
                table: "user_workouts",
                column: "workout_id");

            migrationBuilder.CreateIndex(
                name: "users_email_key",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "users_google_user_id_key",
                table: "users",
                column: "google_user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_workout_exercises_exercise_id",
                table: "workout_exercises",
                column: "exercise_id");

            migrationBuilder.CreateIndex(
                name: "IX_workout_reviews_user_id",
                table: "workout_reviews",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_workout_reviews_workout_id",
                table: "workout_reviews",
                column: "workout_id");

            migrationBuilder.CreateIndex(
                name: "IX_workout_tags_tag_id",
                table: "workout_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_workouts_author_id",
                table: "workouts",
                column: "author_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exercise_media");

            migrationBuilder.DropTable(
                name: "exercise_muscles");

            migrationBuilder.DropTable(
                name: "exercise_reviews");

            migrationBuilder.DropTable(
                name: "exercise_tags");

            migrationBuilder.DropTable(
                name: "user_workouts");

            migrationBuilder.DropTable(
                name: "workout_exercises");

            migrationBuilder.DropTable(
                name: "workout_reviews");

            migrationBuilder.DropTable(
                name: "workout_tags");

            migrationBuilder.DropTable(
                name: "exercises");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "workouts");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
