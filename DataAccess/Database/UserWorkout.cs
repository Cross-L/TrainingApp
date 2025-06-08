using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Database;

[PrimaryKey("UserId", "WorkoutId")]
[Table("user_workouts")]
public partial class UserWorkout
{
    [Key]
    [Column("user_id")]
    public int UserId { get; set; }

    [Key]
    [Column("workout_id")]
    public int WorkoutId { get; set; }

    [Column("added_at", TypeName = "timestamp without time zone")]
    public DateTime? AddedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("UserWorkouts")]
    public virtual User User { get; set; } = null!;

    [ForeignKey("WorkoutId")]
    [InverseProperty("UserWorkouts")]
    public virtual Workout Workout { get; set; } = null!;
}
