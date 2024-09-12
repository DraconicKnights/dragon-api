using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DragonAPI.Model;

[Table("token")]
public class Token
{
    [Key]
    [Column("id")] public long Id { get; set; }
    [Required]
    [Column("node")] public string SessionNode { get; set; }
    [Required]
    [Column("token_creation_date")] public string TokenCreationDate { get; set; }
    [Required]
    [Column("token_created_at")] public string TokenCreatedAt { get; set; }
}