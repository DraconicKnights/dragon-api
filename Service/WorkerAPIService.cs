using System.Text;
using DragonAPI.DataModel;
using Newtonsoft.Json;

namespace DragonAPI.Service;

public class WorkerAPIService
{
    private readonly HttpClient _httpClient;

    public WorkerAPIService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<string> ExecuteCommandAsync(string workerApiUrl, string command)
    {
        var commandRequestData = new CommandRequestModel { Command = command };
        var commandJsonContent = new StringContent(JsonConvert.SerializeObject(commandRequestData), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"{workerApiUrl}/api/Server/ExecuteCommand", commandJsonContent);

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    public async Task StreamLogsAsync(string workerApiUrl, string uuid, Action<string> onLogReceived)
    {
        var response = await _httpClient.GetAsync($"{workerApiUrl}/api/Server/StreamLogs/{uuid}", HttpCompletionOption.ResponseHeadersRead);

        response.EnsureSuccessStatusCode();
        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        string line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            onLogReceived(line);
        }
    }
}