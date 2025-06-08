using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Database;

[Table("workouts")]
public partial class Workout
{
    [Key]
    [Column("workout_id")]
    public int WorkoutId { get; set; }

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("duration")]
    public int? Duration { get; set; }
    

    [Column("intensity")]
    [StringLength(50)]
    public string? Intensity { get; set; }

    [Column("goals")]
    [StringLength(255)]
    public string? Goals { get; set; }
    
    [Column("schedule")]
    [StringLength(255)]
    public string? Schedule { get; set; }

    [Column("level")]
    [StringLength(50)]
    public string? Level { get; set; }

    [Column("tags")]
    [StringLength(255)]
    public string? Tags { get; set; }

    [Column("author_id")]
    public int? AuthorId { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime? CreatedAt { get; set; }

    [Column("average_rating")]
    [Precision(3, 2)]
    public decimal? AverageRating { get; set; }

    [Column("popularity")]
    public int? Popularity { get; set; }

    [Column("calories_burned")]
    public int? CaloriesBurned { get; set; }

    
    [Column("muscle_groups")]
    [StringLength(255)]
    public string? MuscleGroups { get; set; }
    
    [Column("workout_types")]
    [StringLength(255)]
    public string? WorkoutTypes  { get; set; }
    
    [Column("equipment")]
    [StringLength(255)]
    public string? Equipment { get; set; }

    [ForeignKey("AuthorId")]
    [InverseProperty("Workouts")]
    public virtual User? Author { get; set; }
    
    public string AuthorFullName => Author == null
        ? "(Автор не знайдено)"
        : $"{Author.FirstName} {Author.LastName}";
    
    [InverseProperty("Workout")]
    public virtual ICollection<UserWorkout> UserWorkouts { get; set; }

    [InverseProperty("Workout")]
    public virtual ICollection<WorkoutExercise> WorkoutExercises { get; set; }

    [InverseProperty("Workout")]
    public virtual ICollection<WorkoutReview> WorkoutReviews { get; set; }

    [ForeignKey("WorkoutId")]
    [InverseProperty("Workouts")]
    public virtual ICollection<Tag> TagsNavigation { get; set; }
}
