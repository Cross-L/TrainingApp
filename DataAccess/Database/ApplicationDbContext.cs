using Microsoft.EntityFrameworkCore;

namespace DataAccess.Database;

public partial class ApplicationDbContext : DbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Exercise> Exercises { get; set; }

    public virtual DbSet<ExerciseMedia> ExerciseMedia { get; set; }
    
    public virtual DbSet<Tag> Tags { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserWorkout> UserWorkouts { get; set; }

    public virtual DbSet<Workout> Workouts { get; set; }

    public virtual DbSet<WorkoutExercise> WorkoutExercises { get; set; }

    public virtual DbSet<WorkoutReview> WorkoutReviews { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Exercise>(entity =>
        {
            entity.HasKey(e => e.ExerciseId).HasName("exercises_pkey");

            entity.Property(e => e.AverageRating).HasDefaultValueSql("0");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Popularity).HasDefaultValue(0);

            entity.HasOne(d => d.Author).WithMany(p => p.Exercises).HasConstraintName("exercises_author_id_fkey");

            entity.HasMany(d => d.TagsNavigation).WithMany(p => p.Exercises)
                .UsingEntity<Dictionary<string, object>>(
                    "ExerciseTag",
                    r => r.HasOne<Tag>().WithMany()
                        .HasForeignKey("TagId")
                        .HasConstraintName("exercise_tags_tag_id_fkey"),
                    l => l.HasOne<Exercise>().WithMany()
                        .HasForeignKey("ExerciseId")
                        .HasConstraintName("exercise_tags_exercise_id_fkey"),
                    j =>
                    {
                        j.HasKey("ExerciseId", "TagId").HasName("exercise_tags_pkey");
                        j.ToTable("exercise_tags");
                        j.IndexerProperty<int>("ExerciseId").HasColumnName("exercise_id");
                        j.IndexerProperty<int>("TagId").HasColumnName("tag_id");
                    });
        });

        modelBuilder.Entity<ExerciseMedia>(entity =>
        {
            entity.HasKey(e => e.MediaId).HasName("exercise_media_pkey");

            entity.Property(e => e.UploadedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Exercise).WithMany(p => p.ExerciseMedia)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("exercise_media_exercise_id_fkey");
        });
        
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.TagId).HasName("tags_pkey");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("users_pkey");

            entity.Property(e => e.RegistrationDate).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<UserWorkout>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.WorkoutId }).HasName("user_workouts_pkey");

            entity.Property(e => e.AddedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.User).WithMany(p => p.UserWorkouts).HasConstraintName("user_workouts_user_id_fkey");

            entity.HasOne(d => d.Workout).WithMany(p => p.UserWorkouts).HasConstraintName("user_workouts_workout_id_fkey");
        });

        modelBuilder.Entity<Workout>(entity =>
        {
            entity.HasKey(e => e.WorkoutId).HasName("workouts_pkey");

            entity.Property(e => e.AverageRating).HasDefaultValueSql("0");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Popularity).HasDefaultValue(0);

            entity.HasOne(d => d.Author).WithMany(p => p.Workouts).HasConstraintName("workouts_author_id_fkey");

            entity.HasMany(d => d.TagsNavigation).WithMany(p => p.Workouts)
                .UsingEntity<Dictionary<string, object>>(
                    "WorkoutTag",
                    r => r.HasOne<Tag>().WithMany()
                        .HasForeignKey("TagId")
                        .HasConstraintName("workout_tags_tag_id_fkey"),
                    l => l.HasOne<Workout>().WithMany()
                        .HasForeignKey("WorkoutId")
                        .HasConstraintName("workout_tags_workout_id_fkey"),
                    j =>
                    {
                        j.HasKey("WorkoutId", "TagId").HasName("workout_tags_pkey");
                        j.ToTable("workout_tags");
                        j.IndexerProperty<int>("WorkoutId").HasColumnName("workout_id");
                        j.IndexerProperty<int>("TagId").HasColumnName("tag_id");
                    });
        });

        modelBuilder.Entity<WorkoutExercise>(entity =>
        {
            entity.HasKey(e => new { e.WorkoutId, e.ExerciseId }).HasName("workout_exercises_pkey");

            entity.HasOne(d => d.Exercise).WithMany(p => p.WorkoutExercises)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("workout_exercises_exercise_id_fkey");

            entity.HasOne(d => d.Workout).WithMany(p => p.WorkoutExercises).HasConstraintName("workout_exercises_workout_id_fkey");
        });

        modelBuilder.Entity<WorkoutReview>(entity =>
        {
            entity.HasKey(e => e.ReviewId).HasName("workout_reviews_pkey");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.User).WithMany(p => p.WorkoutReviews)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("workout_reviews_user_id_fkey");

            entity.HasOne(d => d.Workout).WithMany(p => p.WorkoutReviews)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("workout_reviews_workout_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    
}
