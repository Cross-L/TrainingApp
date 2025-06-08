using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Database;

[Table("workout_reviews")]
public partial class WorkoutReview
{
    [Key]
    [Column("review_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ReviewId { get; set; }


    [Column("workout_id")]
    public int? WorkoutId { get; set; }

    [Column("user_id")]
    public int? UserId { get; set; }

    [Column("rating")]
    public int? Rating { get; set; }

    [Column("comment")]
    public string? Comment { get; set; }

    [Column("created_at", TypeName = "timestamp without time zone")]
    public DateTime? CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("WorkoutReviews")]
    public virtual User? User { get; set; }

    [ForeignKey("WorkoutId")]
    [InverseProperty("WorkoutReviews")]
    public virtual Workout? Workout { get; set; }
}
