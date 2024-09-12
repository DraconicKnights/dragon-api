using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DragonAPI.Model;

[Table("valid_token_user")]
public class ValidTokenUser
{
    [Key]
    [Column("id")] public long Id { get; set; }
    [Required]
    [Column("token_hash")] public string TokenHash { get; set; }
    [Required]
    [Column("valid_token_user_date")] public string ValidTokenUserCreationDate { get; set; }
    [Column("created_at")] public string ValidTokenUserCreatedAt { get; set; }
}