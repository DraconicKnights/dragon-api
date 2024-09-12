namespace DragonAPI.DataModel;

public class CreateServerWorker
{
    public string ServerName { get; set; }
    public Dictionary<string, string> Parameters { get; set; }
    public int Memory { get; set; }
    public int Storage { get; set; }
}