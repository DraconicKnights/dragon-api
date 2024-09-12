using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DragonAPI.Model;

[Table("account")]
public class Account
{
    [Key]
    [Column("id")] public long Id { get; set; }
    [Required]
    [Column("username")] public string AccountName { get; set; }
    [Required]
    [Column("password")] public string AccountPassword { get; set; }
    [Required]
    [Column("email")] public string AccountEmail { get; set; }
    [Column("serviceTerms")] public bool ServiceTerms { get; set; }
    [Column("userTokens")] public int UserTokens { get; set; }
    [Column("isStaff")] public bool IsStaff { get; set; }
    [Column("adminOverride")] public bool AdminOverride { get; set; }
    public ICollection<Servers> Servers { get; set; }

}