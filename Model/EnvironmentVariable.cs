using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DragonAPI.Model;

[Table("environment")]
public class EnvironmentVariable
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Required]
    [Column("key")]
    public string Key { get; set; }
    
    [Required]
    [Column("value")]
    public string Value { get; set; }
    
    [ForeignKey("gameTypeId")]
    public int? GameTypeId { get; set; }

    public GameTypes GameType { get; set; }
    
    [Column("isGlobal")]
    public bool IsGlobal { get; set; } 
}