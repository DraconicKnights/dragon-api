using DragonAPI.Context;
using DragonAPI.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DragonAPI.Controller;

[ApiController]
[Route("api/[controller]")]
public class PlayerController(RuinDbContext context) : ControllerBase
{
    [HttpGet("{steamId}")]
    public async Task<ActionResult<GlobalPlayerAccount>> GetGlobalPlayerAccount(ulong steamId)
    {
        var account = await context.GlobalPlayerAccounts
            .FirstOrDefaultAsync(a => a.PlayerSteamId == steamId);

        if (account == null)
        {
            return NotFound(new { Message = "Player not found." });
        }

        return Ok(account);
    }
    
    [HttpPost]
    public async Task<ActionResult<GlobalPlayerAccount>> CreateGlobalPlayerAccount(GlobalPlayerAccount account)
    {
        context.GlobalPlayerAccounts.Add(account);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetGlobalPlayerAccount), new { steamId = account.PlayerSteamId }, account);
    }
    
    [HttpPut("{steamId}")]
    public async Task<IActionResult> UpdateGlobalPlayerAccount(ulong steamId, GlobalPlayerAccount updatedAccount)
    {
        var account = await context.GlobalPlayerAccounts.FirstOrDefaultAsync(a => a.PlayerSteamId == steamId);
        
        if (account == null)
        {
            return NotFound(new { Message = "Player not found." });
        }

        // Update account properties
        account.PlayerName = updatedAccount.PlayerName;
        account.IsStaff = updatedAccount.IsStaff;
        account.GlobalBan = updatedAccount.GlobalBan;

        await context.SaveChangesAsync();
        return NoContent();
    }

}