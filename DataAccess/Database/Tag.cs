using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Database;

[Table("tags")]
[Index("Name", Name = "tags_name_key", IsUnique = true)]
public partial class Tag
{
    [Key]
    [Column("tag_id")]
    public int TagId { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string? Name { get; set; }

    [ForeignKey("TagId")]
    [InverseProperty("TagsNavigation")]
    public virtual ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();

    [ForeignKey("TagId")]
    [InverseProperty("TagsNavigation")]
    public virtual ICollection<Workout> Workouts { get; set; } = new List<Workout>();
}
