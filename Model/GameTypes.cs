using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DragonAPI.Model;

[Table("gametypes")]
public class GameTypes
{
    public GameTypes()
    {
        AllowedCommands = new List<AllowedCommand>();
        Servers = new List<Servers>();
        EnvironmentVariables = new List<EnvironmentVariable>();
        SpecialArguments = new List<SpecialArguments>();
        SetupScripts = new List<SetupScript>();
    }
    
    [Key]
    [Column("id")]
    public int Id { get; set; }
  
    [Required]
    [Column("gameName")]
    public string GameName { get; set; }
  
    [Required]
    [Column("dockerImage")]
    public string DockerImage { get; set; }
    
    [Required]
    [Column("dockerCommandTemplate")]
    public string DockerCommandTemplate { get; set; }
    
    [Column("createdTime")]
    public DateTime CreatedDateTime { get; set; }
    
    [Column("updatedTime")]
    public DateTime UpdateDateTime { get; set; }
    
    public ICollection<Servers> Servers { get; set; }
    public ICollection<AllowedCommand> AllowedCommands { get; set; }
    public ICollection<EnvironmentVariable> EnvironmentVariables { get; set; }
    public ICollection<SpecialArguments> SpecialArguments { get; set; }
    public ICollection<SetupScript> SetupScripts { get; set; }
}