using DragonAPI.Context;
using DragonAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DragonAPI.Controller;

/// <summary>
/// Global Group Controller
/// </summary>
/// <param name="context"></param>
[ApiController]
[Route("api/[controller]")]
public class GlobalGroupController(RuinDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<GlobalGroups>>> GetAllGroups()
    {
        var groups = await context.GlobalGroups.ToListAsync();
        return Ok(groups);
    }
    
    [HttpGet("{groupName}")]
    public async Task<ActionResult<GlobalGroups>> GetGroup(string groupName)
    {
        var group = await context.GlobalGroups.FirstOrDefaultAsync(g => g.GroupName == groupName);
        if (group == null)
        {
            return NotFound(new { Message = "Group not found." });
        }
        return Ok(group);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<GlobalGroups>> CreateGroup(GlobalGroups newGroup)
    {
        context.GlobalGroups.Add(newGroup);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetGroup), new { groupName = newGroup.GroupName }, newGroup);
    }

    [Authorize]
    [HttpPut("{groupName}")]
    public async Task<IActionResult> UpdateGroup(string groupName, GlobalGroups updatedGroup)
    {
        var group = await context.GlobalGroups.FirstOrDefaultAsync(g => g.GroupName == groupName);
        if (group == null)
        {
            return NotFound(new { Message = "Group not found." });
        }

        // Update properties
        group.GroupName = updatedGroup.GroupName;
        group.GroupBadge = updatedGroup.GroupBadge;
        group.GroupColour = updatedGroup.GroupColour;
        group.Permissions = updatedGroup.Permissions;
        group.ParentGroupName = updatedGroup.ParentGroupName;

        await context.SaveChangesAsync();
        return NoContent();
    }

    [Authorize]
    [HttpDelete("{groupName}")]
    public async Task<IActionResult> DeleteGroup(string groupName)
    {
        var group = await context.GlobalGroups.FirstOrDefaultAsync(g => g.GroupName == groupName);
        if (group == null)
        {
            return NotFound(new { Message = "Group not found." });
        }

        context.GlobalGroups.Remove(group);
        await context.SaveChangesAsync();
        return NoContent();
    }
}