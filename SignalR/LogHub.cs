using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using DragonAPI.Context;
using DragonAPI.DataModel;
using DragonAPI.Server;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DragonAPI.SignalR;

public class LogHub : Hub
{
    public async Task StartStreamingLogs(string uuid)
    {
        await StreamLogs(Context, uuid);
    }

    private async Task StreamLogs(HubCallerContext context, string uuid)
    {
     using var scope = context.GetHttpContext().RequestServices.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<RuinDbContext>();
        var server = await dbContext.Servers.Include(s => s.Nodes).SingleOrDefaultAsync(s => s.ServerUUID == uuid);
        if (server == null)
        {
            await Clients.All.SendAsync("ReceiveMessage", "Server not found");
            Core.Instance.Logger.Log(LogLevel.Information, "Server not found");
            return;
        }

        var serverName = $"{uuid}";
        var logFilePath = $"{Core.Instance.Configuration["Data:ServerPath"]}{serverName}/logs/latest.log";
        
        Core.Instance.Logger.Log(LogLevel.Information, $"Log file path: {logFilePath}");

        var bashCommand = $"tail -f {logFilePath}";
        
        Core.Instance.Logger.Log(LogLevel.Information, $"Bash command: {bashCommand}");

        using var httpClient = new HttpClient();
        string bearerToken = Core.Instance.Configuration["Data:WorkerToken"];
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        Core.Instance.Logger.Log(LogLevel.Information, "Attempting to retrieve server logs...");

        var commandRequestData = new CommandRequestModel { Command = bashCommand };
        var commandJsonContent = new StringContent(JsonConvert.SerializeObject(commandRequestData), Encoding.UTF8, "application/json");

        try
        {
            var response = await httpClient.PostAsync($"https://{server.Nodes.NodeAddress}/api/Server/NodeCommandAction", commandJsonContent);

            Core.Instance.Logger.Log(LogLevel.Information, $"Response status code: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Core.Instance.Logger.Log(LogLevel.Error, $"Node command action failed. StatusCode: {response.StatusCode}, Error: {errorContent}");
                await Clients.All.SendAsync("ReceiveMessage", "Could not reach Node");
                return;
            }
            
            Core.Instance.Logger.Log(LogLevel.Information, "Successfully retrieved server logs.");

            var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                // If the log line contains [Server thread/INFO]: <username> message, send it to the client
                // This is here purely for testing purposes atm and will most likely be changed
                if (Regex.IsMatch(line, @"\[Server thread/INFO\]: <.*> .*"))
                {
                    await Clients.All.SendAsync("ReceiveMessage", "[LOG] " + line);
                }
            }
        }
        catch (Exception ex)
        {
            Core.Instance.Logger.Log(LogLevel.Error, ex, "An error occurred while attempting to retrieve server logs.");
            await Clients.All.SendAsync("ReceiveMessage", "An error occurred while attempting to retrieve server logs.");
        }
    }
    
}