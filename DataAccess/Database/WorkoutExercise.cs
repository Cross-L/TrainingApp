using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Database;

[PrimaryKey("WorkoutId", "ExerciseId")]
[Table("workout_exercises")]
public partial class WorkoutExercise
{
    [Key]
    [Column("workout_id")]
    public int WorkoutId { get; set; }

    [Key]
    [Column("exercise_id")]
    public int ExerciseId { get; set; }

    [Column("sequence")]
    public int? Sequence { get; set; }

    [Column("reps")]
    public int? Reps { get; set; }

    [Column("sets")]
    public int? Sets { get; set; }

    [Column("rest_time")]
    public int? RestTime { get; set; }

    [ForeignKey("ExerciseId")]
    [InverseProperty("WorkoutExercises")]
    public virtual Exercise Exercise { get; set; } = null!;

    [ForeignKey("WorkoutId")]
    [InverseProperty("WorkoutExercises")]
    public virtual Workout Workout { get; set; } = null!;
}
