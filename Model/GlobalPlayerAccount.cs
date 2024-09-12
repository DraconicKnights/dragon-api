using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DragonAPI.Model;

[Table("globalAccount")]
public class GlobalPlayerAccount
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Column("playerName")]
    public string PlayerName { get; set; }
    
    [Column("playerSteamId")]
    public ulong PlayerSteamId { get; set; }
    
    [Column("deviceId")]
    public string DeviceId { get; set; }
    
    [Column("isStaff")]
    public bool IsStaff { get; set; }
    
    [Column("globalBan")]
    public bool GlobalBan { get; set; }
}