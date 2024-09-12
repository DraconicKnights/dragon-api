using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DragonAPI.Model;

[Table("setup")]
public class SetupScript
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string GameType { get; set; }

    [Required]
    public string ScriptContent { get; set; }
}