using DragonAPI.Context;
using DragonAPI.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DragonAPI.Controller;

/// <summary>
/// Player Controller 
/// </summary>
/// <param name="context"></param>
[ApiController]
[Route("api/[controller]")]
public class PlayerController(RuinDbContext context) : ControllerBase
{
    [HttpGet("{steamId}")]
    public async Task<ActionResult<GlobalPlayerAccount>> GetPlayerAccount(ulong steamId)
    {
        var account = await context.GlobalPlayerAccounts.Include(a => a.GlobalGroups)
            .FirstOrDefaultAsync(a => a.PlayerSteamId == steamId);

        if (account == null)
        {
            return NotFound(new { Message = "Player not found." });
        }

        return Ok(account);
    }
    
    [HttpPost]
    public async Task<ActionResult<GlobalPlayerAccount>> CreateGlobalPlayerAccount([FromBody] GlobalPlayerAccount newAccount)
    {
        // Check if the player already exists
        var existingAccount = await context.GlobalPlayerAccounts
            .FirstOrDefaultAsync(a => a.PlayerSteamId == newAccount.PlayerSteamId);

        if (existingAccount != null)
        {
            return Conflict(new { Message = "Player account already exists." });
        }

        // Add the new account to the context
        context.GlobalPlayerAccounts.Add(newAccount);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPlayerAccount), new { steamId = newAccount.PlayerSteamId }, newAccount);
    }
    
    [HttpPut("{steamId}")]
    public async Task<IActionResult> UpdateGlobalPlayerAccount(ulong steamId, [FromBody] GlobalPlayerAccount updatedAccount)
    {
        var account = await context.GlobalPlayerAccounts.FirstOrDefaultAsync(a => a.PlayerSteamId == steamId);

        if (account == null)
        {
            return NotFound(new { Message = "Player not found." });
        }

        // Update account properties
        account.PlayerName = updatedAccount.PlayerName;
        account.DeviceId = updatedAccount.DeviceId;
        account.IsStaff = updatedAccount.IsStaff;
        account.HasGlobalPerms = updatedAccount.HasGlobalPerms;
        account.GlobalBan = updatedAccount.GlobalBan;
        account.GlobalGroupName = updatedAccount.GlobalGroupName;

        await context.SaveChangesAsync();

        return NoContent();
    }


    [HttpPut("{steamId}/assign-group/{groupName}")]
    public async Task<IActionResult> AssignGroupToPlayer(ulong steamId, string groupName)
    {
        var account = await context.GlobalPlayerAccounts.FirstOrDefaultAsync(a => a.PlayerSteamId == steamId);
        var group = await context.GlobalGroups.FirstOrDefaultAsync(g => g.GroupName == groupName);

        if (account == null)
        {
            return NotFound(new { Message = "Player not found." });
        }
        if (group == null)
        {
            return NotFound(new { Message = "Group not found." });
        }

        // Assign global group to player
        account.GlobalGroupName = group.GroupName;
        account.GlobalGroups.Add(group);
        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{steamId}/remove-group")]
    public async Task<IActionResult> RemoveGroupFromPlayer(ulong steamId)
    {
        var account = await context.GlobalPlayerAccounts.FirstOrDefaultAsync(a => a.PlayerSteamId == steamId);

        if (account == null)
        {
            return NotFound(new { Message = "Player not found." });
        }

        // Remove global group assignment
        account.GlobalGroups = null!;
        await context.SaveChangesAsync();

        return NoContent();
    }

}