using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Database;

[Table("exercise_media")]
public partial class ExerciseMedia
{
    [Key]
    [Column("media_id")]
    public int MediaId { get; set; }

    [Column("exercise_id")]
    public int? ExerciseId { get; set; }

    [Column("media_type")]
    [StringLength(10)]
    public string? MediaType { get; set; }
    
    public string FileName => Path.GetFileName(MediaUrl);
    
    [Column("drive_file_id")]
    [StringLength(255)]
    public string DriveFileId { get; set; }

    [Column("media_url")]
    [StringLength(255)]
    public string? MediaUrl { get; set; }

    [Column("uploaded_at", TypeName = "timestamp without time zone")]
    public DateTime? UploadedAt { get; set; }

    [ForeignKey("ExerciseId")]
    [InverseProperty("ExerciseMedia")]
    public virtual Exercise? Exercise { get; set; }
}
