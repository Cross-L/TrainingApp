using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Database;

[Table("exercises")]
public partial class Exercise
{
    [Key]
    [Column("exercise_id")]
    public int ExerciseId { get; set; }

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("difficulty_level")]
    [StringLength(50)]
    public string? DifficultyLevel { get; set; }

    [Column("exercise_type")]
    [StringLength(50)]
    public string? ExerciseType { get; set; }

    [Column("equipment_needed")]
    [StringLength(255)]
    public string? EquipmentNeeded { get; set; }

    [Column("duration")]
    public int? Duration { get; set; }

    [Column("reps")]
    public int? Reps { get; set; }

    [Column("sets")]
    public int? Sets { get; set; }
    
    [Column("precautions")]
    public string? Precautions { get; set; }

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

    [ForeignKey("AuthorId")]
    [InverseProperty("Exercises")]
    public virtual User? Author { get; set; }

    [InverseProperty("Exercise")]
    public virtual ICollection<ExerciseMedia> ExerciseMedia { get; set; } = new List<ExerciseMedia>();
    
    [InverseProperty("Exercise")]
    public virtual ICollection<WorkoutExercise> WorkoutExercises { get; set; } = new List<WorkoutExercise>();

    [ForeignKey("ExerciseId")]
    [InverseProperty("Exercises")]
    public virtual ICollection<Tag> TagsNavigation { get; set; } = new List<Tag>();
}
