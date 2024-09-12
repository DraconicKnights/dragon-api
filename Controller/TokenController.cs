using DragonAPI.CreationModel;
using DragonAPI.Server;
using Microsoft.AspNetCore.Mvc;

namespace DragonAPI.Controller;

/// <summary>
/// Token controller, used for token request that are then validated
/// </summary>
[ApiController]
[Route("api/token")]
public class TokenController : ControllerBase
{
    [HttpPost]
    public IActionResult Login([FromBody] TokenRequestModel tokenRequestModel)
    {
        Core.Instance.Logger.Log(LogLevel.Information, "API Token request");
        
        Core.Instance.Logger.Log(LogLevel.Information, "Starting API Token request");

        return Ok(Authentication.GenerateToken(tokenRequestModel.Hash, tokenRequestModel.NodeName));
    }
}