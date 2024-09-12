using Newtonsoft.Json;

namespace DragonAPI.CreationModel;

public class CreateGameTypeModel
{
    public string Name { get; set; }
    public string DockerImage { get; set; }
    public List<string> AllowedCommands { get; set; }
    public List<SpecialArgumentModel> SpecialArguments { get; set; }
    public List<EnvironmentVariableModel> EnvironmentVariables { get; set; }
}

public class SpecialArgumentModel
{
    public string Key { get; set; }
    public string Value { get; set; }
    [JsonProperty("Replace")]
    public IDictionary<string, string> ReplaceRules { get; set; } = new Dictionary<string, string>();
    public int GameTypeId { get; set; }
}

public class EnvironmentVariableModel
{
    public string Key { get; set; }
    public string Value { get; set; }
}