using DragonAPI.Context;
using DragonAPI.CreationModel;
using DragonAPI.Model;
using DragonAPI.Server;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace DragonAPI.Controller;

[ApiController]
[Route("api/[controller]")]
public class GamesController(RuinDbContext context) : ControllerBase
{
    [HttpGet]
    [Route("GetGameTypes")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<GameTypes>>> GetAccounts()
    {
        try
        {
            var gameTypes = await context.GameTypes
                .Include(ac => ac.AllowedCommands)
                .Include(gt => gt.EnvironmentVariables)
                .Include(gt => gt.SpecialArguments)
                .AsSplitQuery()
                .ToListAsync();
            return Ok(gameTypes);
        }
        catch (Exception ex)
        {
            Core.Instance.Logger.LogError(ex, "There was an exception while trying to fetch game types.");
            Core.Instance.WriteExceptionToFile(ex);
            return StatusCode(500, "Internal server error. Please check the logs for more details.");
        }
    } 
    
    [HttpPost]
    [Route("CreateGameType")]
    [Authorize]  // Ensure the request is authorized
    public async Task<IActionResult> CreateGameType([FromBody] CreateGameTypeModel createGameTypeModel)
    {
        if (createGameTypeModel == null || createGameTypeModel.AllowedCommands == null)
        {
            return BadRequest("Insufficient parameters");
        }

        var gameType = new GameTypes()
        {
            GameName = createGameTypeModel.Name,
            DockerImage = createGameTypeModel.DockerImage,
            DockerCommandTemplate = "",
            CreatedDateTime = DateTime.Now,
            UpdateDateTime = DateTime.Now
        };

        foreach (var command in createGameTypeModel.AllowedCommands)
        {
            gameType.AllowedCommands.Add(new AllowedCommand
            {
                Command = command,
                GameTypesId = gameType.Id
            });
        }
        
        foreach (var envVar in createGameTypeModel.EnvironmentVariables)
        {
            gameType.EnvironmentVariables.Add(new EnvironmentVariable
            {
                Key = envVar.Key,
                Value = envVar.Value,
                GameTypeId = gameType.Id,
                IsGlobal = false
            });
        }

        
        foreach (var arg in createGameTypeModel.SpecialArguments)
        {
            var replaceRulesJson = JsonConvert.SerializeObject(arg.ReplaceRules);
            
            gameType.SpecialArguments.Add(new SpecialArguments()
            {
                Key = arg.Key,
                Value = arg.Value,
                ReplaceRulesJson = replaceRulesJson,
                GameTypeId = gameType.Id,
            });
        }
        

        try
        {
            context.GameTypes.Add(gameType);
            await context.SaveChangesAsync();
            Core.Instance.Logger.LogInformation("GameType and SpecialArguments saved successfully.");
        }
        catch (Exception ex)
        {
            Core.Instance.Logger.LogError(ex, "Failed to save GameType and SpecialArguments.");
            throw;
        }

        return Ok();
    }
    
    [HttpDelete]
    [Route("DeleteGameType/{gameId}")]
    [Authorize]
    public async Task<ActionResult> DeleteGameType(int gameId)
    {
        try
        {
            var gameToDelete = await context.GameTypes.FindAsync(gameId);
            if (gameToDelete == null)
            {
                return NotFound($"Game with ID {gameId} not found.");
            }

            context.GameTypes.Remove(gameToDelete);
            await context.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error occurred while deleting game: {ex.Message}");
        }
    }
}