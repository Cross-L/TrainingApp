using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Database;

[Table("users")]
[Index("Email", Name = "users_email_key", IsUnique = true)]
[Index("GoogleUserId", Name = "users_google_user_id_key", IsUnique = true)]
public partial class User
{
    [Key]
    [Column("user_id")]
    public int UserId { get; set; }

    [Column("email")]
    [StringLength(255)]
    public string Email { get; set; } = null!;

    [Column("first_name")]
    [StringLength(100)]
    public string? FirstName { get; set; }

    [Column("last_name")]
    [StringLength(100)]
    public string? LastName { get; set; }

    [Column("password")]
    [StringLength(255)]
    public string? Password { get; set; }
    
    [Column("goals")]
    [StringLength(255)]
    public string? Goals { get; set; }

    [Column("age")]
    public int? Age { get; set; }

    [Column("weight")]
    [Precision(5, 2)]
    public decimal? Weight { get; set; }

    [Column("preferred_training_type")]
    [StringLength(100)]
    public string? PreferredTrainingType { get; set; }

    [Column("registered_via")]
    [StringLength(50)]
    public string RegisteredVia { get; set; } = null!;

    [Column("registration_date", TypeName = "timestamp without time zone")]
    public DateTime? RegistrationDate { get; set; }

    [Column("google_user_id")]
    [StringLength(255)]
    public string? GoogleUserId { get; set; }
    
    [InverseProperty("Author")]
    public virtual ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();

    [InverseProperty("User")]
    public virtual ICollection<UserWorkout> UserWorkouts { get; set; } = new List<UserWorkout>();

    [InverseProperty("User")]
    public virtual ICollection<WorkoutReview> WorkoutReviews { get; set; } = new List<WorkoutReview>();

    [InverseProperty("Author")]
    public virtual ICollection<Workout> Workouts { get; set; } = new List<Workout>();
    
    [Column("gender")]
    [StringLength(10)]
    public string? Gender { get; set; }

    [Column("height")]
    [Precision(5, 2)]
    public decimal? Height { get; set; }

    [Column("physical_activity_level")]
    [StringLength(50)]
    public string? PhysicalActivityLevel { get; set; }

}
