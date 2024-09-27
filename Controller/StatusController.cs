using Microsoft.AspNetCore.Mvc;

namespace DragonAPI.Controller;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { status = "UP" });
    }
}