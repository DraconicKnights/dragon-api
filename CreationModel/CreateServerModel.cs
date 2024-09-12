namespace DragonAPI.CreationModel;

public class CreateServerModel
{
    public string GameType { get; set; }
    public int AccountId { get; set; }
    public string UUID { get; set; }
    public string ServerName { get; set; }
    public List<KeyValuePair<string, string>> Settings { get; set; }
    public int MemoryCap { get; set; }
    public int StorageCap { get; set; }
}