using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DragonAPI.Enum;

namespace DragonAPI.Model;

[Table("globalGroups")]
public class GlobalGroups
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("groupName")]
    public string GroupName { get; set; }
    
    [Column("groupBadge")]
    public string GroupBadge { get; set; }
    
    [Column("groupColour")]
    public string GroupColour { get; set; }
    
    [Column("permissions")]
    public List<PlayerPerms> Permissions { get; set; } // Ensure this is serializable
    
    [Column("parentGroupName")]
    public string ParentGroupName { get; set; }
}