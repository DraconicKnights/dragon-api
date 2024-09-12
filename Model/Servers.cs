using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DragonAPI.Model;

[Table("servers")]
public class Servers
{
    [Key]
    [Column("id")] public int Id { get; set; }
    [Required]
    [Column("gameType")] public string GameType { get; set; }
    [Required]
    [Column("serverUUID")] public string ServerUUID { get; set; }
    [Column("serverName")] public string ServerName { get; set; }
    [Column("serverAddress")] public string ServerAddress { get; set; }
    [Column("serverPort")] public int ServerPort { get; set; }
    [Column("dockerImage")] public string DockerImage { get; set; }
    [Column("serverMemory")] public int ServerMemory { get; set; }
    [Column("serverStorage")] public int ServerStorage { get; set; }
    [Column("serverNodeId")] public int NodeId { get; set; }
    [ForeignKey("NodeId")]
    public Nodes Nodes { get; set; }
    
    [Column("gameTypeId")] public int GameTypeId { get; set; }
    [ForeignKey("GameTypeId")]
    public GameTypes GameTypes { get; set; }
    
    [Column("accountId")] public long AccountId { get; set; }
    [ForeignKey("AccountId")]
    public Account Account { get; set; }
}