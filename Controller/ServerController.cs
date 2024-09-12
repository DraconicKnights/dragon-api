using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using DragonAPI.Context;
using DragonAPI.CreationModel;
using DragonAPI.DataModel;
using DragonAPI.Model;
using DragonAPI.Server;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DragonAPI.Controller;

/// <summary>
/// Server controller, deals with server creation and management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ServerController : ControllerBase
{
    private string masterUrl = Core.Instance.Configuration["Data:API_Node_Address"];
    
    private readonly RuinDbContext context;
    private readonly HttpClient httpClient;

    public ServerController(RuinDbContext context)
    {
        this.context = context;
        httpClient = new HttpClient();
        string bearerToken = Core.Instance.Configuration["Data:WorkerToken"];
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
    }
    
    [HttpGet]
    [Route("TestPoint")]
    [Authorize]
    public async Task<IActionResult> GetTest()
    {
        using var httpClient = new HttpClient();
        string bearerToken = Core.Instance.Configuration["Data:WorkerToken"];
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        
        List<Nodes> nodeList = await httpClient.GetFromJsonAsync<List<Nodes>>($"{masterUrl}api/Node/GetNodes");
        if (nodeList == null || nodeList.Count == 0)
        {
            Core.Instance.Logger.Log(LogLevel.Error, "No nodes available.");
            return Problem("No nodes available.");
        }
            
        Nodes node = nodeList.MinBy(x => Guid.NewGuid())!;
        Core.Instance.Logger.Log(LogLevel.Information, $"Node selected: {node.NodeName}");
        Core.Instance.Logger.Log(LogLevel.Information, $"Node Address: {node.NodeAddress}");
        
        var requestUri = $"https://{node.NodeAddress}/api/Server/TestPoint";

        var requestData = new TestModelRequest() { Value = 25 };
        var jsonContent = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

        var portResponse = await httpClient.PostAsync(requestUri, jsonContent);
        
        string responseContent = await portResponse.Content.ReadAsStringAsync();
        Core.Instance.Logger.Log(LogLevel.Information, $"Response content: {responseContent}");
        
        var freePort = await portResponse.Content.ReadFromJsonAsync<int>();
        
        return Ok(freePort);
    } 
    
    [HttpGet]
    [Route("GetServers")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<Servers>>> GetServers()
    {
        try
        {
            var servers = await context.Servers.ToListAsync();

            if (servers == null)
            {
                return NotFound(new { message = "Servers not found." });
            }
            
            return Ok(servers);

        }
        catch (Exception ex)
        {
            Core.Instance.Logger.LogError(ex, "There was an exception while trying to fetch the server list.");
            Core.Instance.WriteExceptionToFile(ex);
            return StatusCode(500, new { message = "An error occurred while processing your request." });
        }

    }

    [HttpGet]
    [Route("GetServer/{uuid}")]
    [Authorize]
    public async Task<ActionResult<Servers>> GetAccount(string uuid)
    {
        try
        {
            var server = await context.Servers
                .Include(sn => sn.Nodes)
                .Include(sg => sg.GameTypes)
                .Include(sa => sa.Account)
                .SingleOrDefaultAsync(s => s.ServerUUID == uuid);

            if (server == null)
            {
                return NotFound(new { message = "Server not found." });
            }

            return Ok(server);
        }
        catch (Exception ex)
        {
            Core.Instance.Logger.LogError(ex, "There was an exception while trying to fetch the server.");
            Core.Instance.WriteExceptionToFile(ex);
            return StatusCode(500, new { message = "An error occurred while processing your request." });
        }
    }
    
    [HttpPost]
    [Route("CreateServer")]
    [Authorize]
    public async Task<IActionResult> CreateServer([FromBody] CreateServerModel model)
    {
        // Fetch game types from master server
        List<GameTypes> gameTypesList;
        try
        {
            gameTypesList = await httpClient.GetFromJsonAsync<List<GameTypes>>($"{masterUrl}api/Games/GetGameTypes");
        }
        catch (Exception ex)
        {
            Core.Instance.Logger.LogError(ex, "Failed to fetch game types from master server.");
            return Problem("Failed to fetch game types from master server.");
        }

        if (gameTypesList == null || gameTypesList.Count == 0)
        {
            Core.Instance.Logger.LogError("No games available.");
            return Problem("No games available.");
        }

        // Validate game type
        var game = gameTypesList.FirstOrDefault(g => g.GameName.Equals(model.GameType, StringComparison.OrdinalIgnoreCase));
        if (game == null)
        {
            Core.Instance.Logger.LogError($"Unsupported game type: {model.GameType}");
            return BadRequest($"Unsupported game type: {model.GameType}");
        }

        var gameType = await context.GameTypes.Include(gt => gt.EnvironmentVariables)
            .Include(gt => gt.SetupScripts)
            .SingleOrDefaultAsync(g => g.GameName == model.GameType);
        
        if (gameType == null)
        {
            Core.Instance.Logger.LogError($"Unsupported game type: {model.GameType}");
            return BadRequest($"Unsupported game type: {model.GameType}");
        }

        // Create server
        var serverName = $"{model.GameType}-server-{model.UUID}";
        var serverUUID = $"{model.GameType}-{Guid.NewGuid():N}";
        var dockerImage = gameType.DockerImage.ToLower();

        // Fetch nodes from master server
        List<Nodes> nodeList;
        try
        {
            nodeList = await httpClient.GetFromJsonAsync<List<Nodes>>($"{masterUrl}api/Node/GetNodes");
        }
        catch (Exception ex)
        {
            Core.Instance.Logger.LogError(ex, "Failed to fetch nodes from master server.");
            return Problem("Failed to fetch nodes from master server.");
        }

        if (nodeList == null || nodeList.Count == 0)
        {
            Core.Instance.Logger.LogError("No nodes available.");
            return Problem("No nodes available.");
        }

        // Select suitable node
        var suitableNode = await SelectSuitableNodeAsync(nodeList, model.MemoryCap, model.StorageCap);
        if (suitableNode == null)
        {
            Core.Instance.Logger.LogWarning("Insufficient resources. No node has enough memory or storage.");
            return BadRequest("No Node has enough memory or storage.");
        }

        Core.Instance.Logger.LogInformation($"Node selected: {suitableNode.NodeName}");
        Core.Instance.Logger.LogInformation($"Node Address: {suitableNode.NodeAddress}");

        // Get available port
        int freePort;
        try
        {
            freePort = await GetAvailablePortAsync(suitableNode, 25670);
        }
        catch (Exception ex)
        {
            Core.Instance.Logger.LogError(ex, "Failed to retrieve available port.");
            return Problem("Failed to retrieve available port.");
        }

        var server = new Servers
        {
            GameType = model.GameType,
            ServerUUID = serverUUID,
            ServerName = model.ServerName,
            ServerAddress = suitableNode.NodeAddress,
            NodeId = suitableNode.Id,
            GameTypeId = gameType.Id,
            ServerPort = freePort,
            DockerImage = dockerImage,
            ServerMemory = model.MemoryCap,
            ServerStorage = model.StorageCap,
            AccountId = model.AccountId
        };

        try
        {
            var exists = context.Accounts.Any(a => a.Id == model.AccountId);
            
            if (exists) 
            {
                context.Servers.Add(server);
                await context.SaveChangesAsync();
            }
        }
        catch (DbUpdateException ex)
        {
            Core.Instance.Logger.LogError(ex, "Database update failed.");
            return Problem("Database update failed.");
        }

        // Build Docker command
        var command = await BuildDockerCommandAsync(model, serverUUID, freePort, game.DockerImage, gameType.Id);
        var response = await ExecuteCommandOnNodeAsync(suitableNode.NodeAddress, command);

        await Task.Delay(15000);

        if (response.IsSuccessStatusCode)
        {
            // Apply special arguments
            var specialArgs = await context.SpecialArguments
                .Where(sa => sa.GameTypeId == gameType.Id)
                .ToListAsync();

            await ApplySpecialArgumentsAsync(suitableNode.NodeAddress, server.ServerUUID, freePort, specialArgs);

            // Restart the server to apply special arguments
            var restartResponse = await ExecuteCommandOnNodeAsync(suitableNode.NodeAddress, $"docker restart {server.ServerUUID}");

            if (restartResponse.IsSuccessStatusCode)
                return Ok("Server setup process initiated. Check provided callback for status updates.");
        
            Core.Instance.Logger.LogError("Failed to restart the server.");
            return Problem("Failed to restart the server.");
        }
        Core.Instance.Logger.LogError("Failed to execute command on node.");
        return Problem("Failed to execute command on node.");

    }

    
    [HttpPost]
    [Route("StartServer")]
    [Authorize]
    public async Task<IActionResult> StartServer(string uuid)
    {
        var server = await context.Servers.Include(s => s.Nodes).SingleOrDefaultAsync(s => s.ServerUUID == uuid);
        if (server == null)
        {
            return NotFound("Server not found");
        }

        var serverName = $"{server.GameType}-server-{uuid}";
        var command = $"docker start {serverName}";
        var response = await ExecuteCommandOnNodeAsync(server.Nodes.NodeAddress, command);

        return response.IsSuccessStatusCode
            ? Ok(new { status = "info", message = $"Server {uuid} is starting." })
            : Problem(new { status = "error", message = "Could not reach Node. Please check the Node Server" }.ToString());
    }

    [HttpPost]
    [Route("StopServer")]
    [Authorize]
    public async Task<IActionResult> StopServer(string uuid)
    {
        var server = await context.Servers.Include(s => s.Nodes).SingleOrDefaultAsync(s => s.ServerUUID == uuid);
        if (server == null)
        {
            return NotFound("Server not found");
        }

        var serverName = $"{server.GameType}-server-{uuid}";
        var command = $"docker stop {serverName}";
        var response = await ExecuteCommandOnNodeAsync(server.Nodes.NodeAddress, command);

        return response.IsSuccessStatusCode
            ? Ok(new { status = "info", message = $"Server {uuid} is stopping." })
            : Problem(new { status = "error", message = "Could not reach Node. Please check the Node Server" }.ToString());
    }

    [HttpPost]
    [Route("RestartServer")]
    [Authorize]
    public async Task<IActionResult> RestartServer(string uuid)
    {
        var server = await context.Servers.Include(s => s.Nodes).SingleOrDefaultAsync(s => s.ServerUUID == uuid);
        if (server == null)
        {
            return NotFound("Server not found");
        }

        var serverName = $"{server.GameType}-server-{uuid}";
        var command = $"docker stop {serverName} && docker start {serverName}";
        var response = await ExecuteCommandOnNodeAsync(server.Nodes.NodeAddress, command);

        return response.IsSuccessStatusCode
            ? Ok(new { status = "info", message = $"Server {uuid} is restarting." })
            : Problem(new { status = "error", message = "Could not reach Node. Please check the Node Server" }.ToString());
    }
    
    [HttpGet]
    [Route("GetServerStatus")]
    [Authorize]
    public async Task<IActionResult> GetServerStatus(string uuid)
    {
        var server = await context.Servers.Include(s => s.Nodes).SingleOrDefaultAsync(s => s.ServerUUID == uuid);
        if (server == null)
        {
            return NotFound(new { message = "Server not found." });
        }

        var serverName = $"{server.GameType}-server-{uuid}";
        var command = $"docker inspect -f '{{{{.State.Running}}}}' {serverName}";
        var response = await ExecuteCommandOnNodeAsync(server.Nodes.NodeAddress, command);

        if (!response.IsSuccessStatusCode)
        {
            return Problem("An error occurred while retrieving the server status.");
        }

        var output = await response.Content.ReadAsStringAsync();
        bool isRunning = output.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);

        return Ok(new { serverStatus = isRunning ? "ready" : "not ready" });
    }
    
    [HttpGet]
    [Route("GetServerStats/{uuid}")]
    [Authorize]
    public async Task<IActionResult> GetServerStats(string uuid)
    {
        var server = await context.Servers.Include(s => s.Nodes).SingleOrDefaultAsync(s => s.ServerUUID == uuid);
        if (server == null)
        {
            return NotFound("Server not found");
        }

        var serverName = $"{server.GameType}-server-{uuid}";
        var command = $"docker stats {serverName} --no-stream";
        var response = await ExecuteCommandOnNodeAsync(server.Nodes.NodeAddress, command);

        if (!response.IsSuccessStatusCode)
        {
            return Problem("An error occurred while retrieving the server stats.");
        }

        var stats = await response.Content.ReadAsStringAsync();
        // TODO: Parse the 'stats' string to retrieve memory usage and other information

        return Ok(stats);
    }
    
    /*[HttpGet]
    [Route("GetServerLogs")]
    [Authorize]
    public async Task<IActionResult> GetServerLogs(string uuid)
    {
        if (HttpContext.WebSockets.IsWebSocketRequest)
        {
            using (var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync())
            {
                await APIBuildCore.StreamLogs(HttpContext, webSocket);
            }
            return new EmptyResult();
        }
        else
        {
            return BadRequest("WebSocket connection required");
        }
    }*/
    
    [HttpPost]
    [Route("SendServerCommand")]
    [Authorize]
    public async Task<IActionResult> SendServerCommand(string uuid, string cmd)
    {
        var server = await context.Servers
            .Include(s => s.Nodes)
            .Include(s => s.GameTypes)
            .ThenInclude(g => g.AllowedCommands)
            .SingleOrDefaultAsync(s => s.ServerUUID == uuid);

        if (server == null)
        {
            return NotFound("Server not found");
        }

        var allowedCommandList = server.GameTypes.AllowedCommands.Select(ac => ac.Command).ToList();
        var validatedCommand = ValidateCommand(allowedCommandList, cmd);

        if (validatedCommand == null)
        {
            return BadRequest(new { status = "invalid", message = "Invalid Command" });
        }

        var serverName = $"{server.GameType}-server-{uuid}";
        var command = $"docker exec -i {serverName} rcon-cli {validatedCommand}";
        var response = await ExecuteCommandOnNodeAsync(server.Nodes.NodeAddress, command);

        return response.IsSuccessStatusCode
            ? Ok(new { status = "info", message = $"Command '{validatedCommand}' sent to server {uuid}." })
            : Problem(new { status = "error", message = "Could not reach Node. Please check the Node Server" }.ToString());
    }
    
    [HttpDelete]
    [Route("DeleteServer/{uuid}")]
    [Authorize]
    public async Task<IActionResult> DeleteServer(string uuid)
    {
        var server = await context.Servers.Include(s => s.Nodes).SingleOrDefaultAsync(s => s.ServerUUID == uuid);
        if (server == null)
        {
            return NotFound("Server not found.");
        }

        var node = await context.Nodes.Include(n => n.Servers).SingleOrDefaultAsync(n => n.Id == server.NodeId);
        if (node == null)
        {
            return NotFound("Node not found.");
        }

        node.UsedMemory -= server.ServerMemory;
        node.UsedStorage -= server.ServerStorage;
        node.ActiveServers--;

        context.Nodes.Update(node);
        await context.SaveChangesAsync();

        var serverName = $"{uuid}";
        var serverPort = server.ServerPort;
        var command = $"docker stop {serverName} && docker rm {serverName} && (sudo iptables -C INPUT -p tcp --dport {serverPort} -j ACCEPT && sudo iptables -D INPUT -p tcp --dport {serverPort} -j ACCEPT || echo 'No such iptables rule') && sudo rm -rf {Core.Instance.Configuration["Data:ServerPath"]}{serverName}";
        var response = await ExecuteCommandOnNodeAsync(server.Nodes.NodeAddress, command);

        if (!response.IsSuccessStatusCode)
        {
            return Problem("Could not reach Node. Please check the Node Server.");
        }

        context.Servers.Remove(server);
        await context.SaveChangesAsync();

        return Ok($"Server {uuid} has been deleted.");
    }
    
    private async Task<Nodes> SelectSuitableNodeAsync(IEnumerable<Nodes> nodes, int memoryCap, int storageCap)
    {
        foreach (var node in nodes)
        {
            var currentNode = await context.Nodes.Include(n => n.Servers)
                                                 .SingleAsync(n => n.Id == node.Id);

            var totalMemoryUsed = currentNode.Servers.Sum(s => s.ServerMemory);
            var totalStorageUsed = currentNode.Servers.Sum(s => s.ServerStorage);
            var remainingMemory = currentNode.TotalMemory - totalMemoryUsed;
            var remainingStorage = currentNode.TotalStorage - totalStorageUsed;

            if (memoryCap <= remainingMemory && storageCap <= remainingStorage)
            {
                currentNode.UsedMemory += memoryCap;
                currentNode.UsedStorage += storageCap;
                currentNode.ActiveServers++;

                context.Nodes.Update(currentNode);
                await context.SaveChangesAsync();
                return currentNode;
            }
        }

        return null!;
    }

    private async Task<int> GetAvailablePortAsync(Nodes node, int startingPort)
    {
        var requestUri = $"https://{node.NodeAddress}/api/Server/NodeCommandGetPort";
        var requestData = new PortRequestModel { StartingPort = startingPort };
        var jsonContent = new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(requestUri, jsonContent);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<int>();
    }

    private async Task<string> BuildDockerCommandAsync(CreateServerModel model, string serverName, int port, string dockerImage, int gameTypeId)
    {
        var globalEnvironmentVariables = await context.EnvironmentVariables
            .Where(e => e.IsGlobal)
            .ToListAsync();

        var gameSpecificEnvironmentVariables = await context.EnvironmentVariables
            .Where(e => e.GameTypeId == gameTypeId)
            .ToListAsync();

        var allEnvironmentVariables = globalEnvironmentVariables.Concat(gameSpecificEnvironmentVariables);
        
        // Fetch special arguments for the game type
        var specialArgs = await context.SpecialArguments
            .Where(sa => sa.GameTypeId == gameTypeId)
            .ToListAsync();

        var parametersStr = allEnvironmentVariables.Aggregate(string.Empty, (current, variable) => 
            current + $"-e {variable.Key}={variable.Value} ");


        var createDirectoryCommand = $"mkdir -p {Core.Instance.Configuration["Data:ServerPath"]}{serverName}";

        var dockerCommand = $"{createDirectoryCommand} && docker run -d -p {port}:{port} " +
                            $"-m {model.MemoryCap}m --memory-swap {model.MemoryCap}m " +
                            $"-v {Core.Instance.Configuration["Data:ServerPath"]}{serverName}:/data " +
                            $"--name {serverName} {parametersStr} {dockerImage} " +
                            $"&& sudo iptables -A INPUT -p tcp --dport {port} -j ACCEPT";

        return dockerCommand;
        
        /*var parametersStr = model.Settings.Aggregate(" ", (current, setting) => current + $"-e {setting.Key}={setting.Value} ");
        
        var createDirectoryCommand = $"mkdir -p {Core.Instance.Configuration["Data:ServerPath"]}{serverName}";

        var dockerCommand = $"{createDirectoryCommand} && docker run -d -p {port}:{port} -m {model.MemoryCap}m --memory-swap {model.MemoryCap}m -v {Core.Instance.Configuration["Data:ServerPath"]}{serverName}:/data --name {serverName} -e SERVER_PORT={port} {parametersStr} {dockerImage} && sudo iptables -A INPUT -p tcp --dport {port} -j ACCEPT";

        return dockerCommand;*/
    }
    
    private async Task ApplySpecialArgumentsAsync(string nodeAddress, string serverUuid, int port, IEnumerable<SpecialArguments> specialArgs)
    {
        foreach (var arg in specialArgs)
        {
            try
            {
                var value = arg.Value
                    .Replace("{serverPath}", Core.Instance.Configuration["Data:ServerPath"] + serverUuid)
                    .Replace("{port}", port.ToString());
                
                Core.Instance.Logger.LogInformation($"Processing special argument for file: {value}");
                Core.Instance.Logger.LogInformation($"Replace rules JSON: {arg.ReplaceRulesJson}");

                var replaceRules = JsonConvert.DeserializeObject<IDictionary<string, string>>(arg.ReplaceRulesJson);
                if (replaceRules == null)
                {
                    Core.Instance.Logger.LogError($"Failed to deserialize replace rules JSON: {arg.ReplaceRulesJson}");
                    continue;
                }
                Core.Instance.Logger.LogInformation($"Deserialized replace rules: {string.Join(", ", replaceRules.Select(r => $"{r.Key} => {r.Value}"))}");

                var replaceCommands = replaceRules
                    .Select(r =>
                    {
                        var escapeKey = Regex.Escape(r.Key);
                        var keyPattern = $"^{escapeKey}=.*";
                        var replacedValue = $"{r.Key}={r.Value.Replace("{port", port.ToString())}";
                        var command = $"sed -i 's/{keyPattern}/{replacedValue}/' {value}";
                        Console.WriteLine($"Generated command: {command}");
                        return command;
                    })
                    .ToList();
                
                var replaceCommand = string.Join(" && ", replaceCommands);

                if (string.IsNullOrEmpty(replaceCommand))
                {
                    Core.Instance.Logger.LogError("Replace commands are empty.");
                    continue;
                }

                var applySpecialArgsCommand = $"sleep 15 && {replaceCommand}";

                Core.Instance.Logger.LogInformation($"Executing command: {applySpecialArgsCommand}");

                var response = await ExecuteCommandOnNodeAsync(nodeAddress, applySpecialArgsCommand);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Core.Instance.Logger.LogError($"Command failed with status code {response.StatusCode}: {responseContent}");
                    throw new Exception($"Failed to apply special arguments to server. Command: {applySpecialArgsCommand}, Response: {responseContent}");
                }

                Core.Instance.Logger.LogInformation("Special arguments applied successfully.");
            }
            catch (Exception ex)
            {
                Core.Instance.Logger.LogError(ex, "Exception occurred while applying special arguments.");
                throw;
            }
        }
    }




    private async Task<HttpResponseMessage> ExecuteCommandOnNodeAsync(string nodeAddress, string command)
    {
        var commandRequestData = new CommandRequestModel { Command = command };
        var commandJsonContent = new StringContent(JsonConvert.SerializeObject(commandRequestData), Encoding.UTF8, "application/json");
        var commandRequestUri = $"https://{nodeAddress}/api/Server/NodeCommand";

        return await httpClient.PostAsync(commandRequestUri, commandJsonContent);
    }
    
    private string ValidateCommand(List<string> allowedCommands, string cmd)
    {
        if (allowedCommands == null || !allowedCommands.Contains(cmd.Trim().ToLower()))
        {
            return null;
        }

        return cmd;
    }
}
