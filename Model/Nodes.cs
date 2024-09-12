using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DragonAPI.Enums;

namespace DragonAPI.Model;

[Table("nodes")]
public class Nodes
{
    public Nodes()
    {
        Servers = new List<Servers>();
    }
    
    [Key]
    [Column("id")]
    public int Id { get; set; }
  
    [Required]
    [Column("nodeName")]
    public string NodeName { get; set; }
  
    [Required]
    [Column("nodeAddress")]
    public string NodeAddress { get; set; }
    
    [Required]
    [Column("nodeState")]
    public NodeState NodeState { get; set; }
  
    [Required]
    [Column("lastChecked")]
    public DateTime LastChecked { get; set; }
    
    [Required]
    [Column("memoryCap")]
    public int TotalMemory { get; set; }
    
    [Required]
    [Column("usedMemory")]
    public int UsedMemory { get; set; }
    
    [Required]
    [Column("storageCap")]
    public int TotalStorage { get; set; }
    
    [Required]
    [Column("usedStorage")]
    public int UsedStorage { get; set; }
    
    [Column("activeServers")] 
    public int ActiveServers { get; set; }
    public ICollection<Servers> Servers { get; set; }
}