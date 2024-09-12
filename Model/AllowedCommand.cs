using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DragonAPI.Model;

[Table("AllowedCommands")]
public class AllowedCommand
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("command")]
    public string Command { get; set; }
    [Column("gameTypesId")]
    public int GameTypesId { get; set; }
    [ForeignKey("GameTypesId")]
    public GameTypes GameTypes { get; set; }
}