using System.Diagnostics;
using DragonAPI.Context;
using DragonAPI.CreationModel;
using DragonAPI.Enums;
using DragonAPI.Model;
using DragonAPI.Server;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DragonAPI.Controller;

/// <summary>
/// Node Controller, Deals with server nodes and registration requests
/// </summary>
/// <param name="context"></param>
[ApiController]
[Route("api/[controller]")]
public class NodeController(RuinDbContext context) : ControllerBase
{
    /*
    [HttpPost]
    [Route("Register")]
    [Authorize]
    public async Task<IActionResult> RegisterNode([FromBody] NodeRegistrationRequest request)
    {
        var node = await context.Nodes.SingleOrDefaultAsync(n => n.OneTimeCode == request.OneTimeCode && n.NodeAddress == request.NodeAddress);
        
        if (node == null)
        {
            return BadRequest("Invalid or expired one-time code, or mismatched node address.");
        }

        // Mark node as registered
        node.IsRegistered = true;
        node.OneTimeCode = null;  // Invalidate the OTC after use
        await context.SaveChangesAsync();

        return Ok(new { success = true, message = "Node registered successfully. Please proceed to configure the node." });
    }
    */

    
    [HttpGet]
    [Route("GetNodes")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<Nodes>>> GetAccounts()
    {
        try
        {
            var nodes = await context.Nodes
                .Include(ns => ns.Servers)
                .AsSplitQuery()
                .ToListAsync();
            return Ok(nodes);
        }
        catch (Exception ex)
        {
            Core.Instance.Logger.LogError(ex, "There was an exception while trying to fetch game types.");
            Core.Instance.WriteExceptionToFile(ex);
            return StatusCode(500, "Internal server error. Please check the logs for more details.");
        }
    }
    
    [HttpPost]
    [Route("CreateNode")]
    [Authorize]  // Ensure the request is authorized
    public async Task<IActionResult> CreateNode([FromBody] CreateNodeModel createNodeModel)
    {
        try
        {
            var node = new Nodes
            {
                NodeName = createNodeModel.NodeName,
                NodeAddress = createNodeModel.NodeAddress,
                NodeState = NodeState.Active,
                LastChecked = DateTime.Now,
                TotalMemory = createNodeModel.MemoryCap,
                TotalStorage = createNodeModel.StorageCap
            };

            context.Nodes.Add(node);
            await context.SaveChangesAsync();

            string allowOutgoingCommand = $"sudo iptables -A OUTPUT -d {node.NodeAddress} -p tcp --dport 8000 -j ACCEPT";
            string allowIncomingCommand = $"sudo iptables -A INPUT -s {node.NodeAddress} -p tcp --sport 8000 -m state --state ESTABLISHED,RELATED -j ACCEPT";

            await ExecuteShellCommandAsync(allowOutgoingCommand);
            await ExecuteShellCommandAsync(allowIncomingCommand);

            return Ok();
        }
        catch (Exception ex)
        {
            Core.Instance.Logger.LogError(ex, "There was an exception while trying to create a node.");
            Core.Instance.WriteExceptionToFile(ex);
            return StatusCode(500, "Internal server error. Please check the logs for more details.");
        }
    }
    
    [HttpGet]
    [Route("SelectNode")]
    [Authorize]  // Ensure the request is authorized
    public ActionResult<Nodes> SelectNodeForServer(int requiredMemory, int requiredStorage)
    {
        var suitableNode = context.Nodes
            .Where(node => node.NodeState == NodeState.Active &&
                           node.TotalMemory >= requiredMemory &&
                           node.TotalStorage >= requiredStorage)
            .OrderBy(node => node.Id)  // ordering by id
            .FirstOrDefault();

        if (suitableNode == null)
        {
            return NotFound(new { message = "No suitable node available." });
        }

        return Ok(suitableNode);
    }
    
    [HttpPut]
    [Route("UpdateNode")]
    [Authorize]
    public async Task<ActionResult> UpdateNode([FromBody] Nodes updatedNode)
    {
        try
        {
            context.Entry(updatedNode).State = EntityState.Modified;
            await context.SaveChangesAsync();
            return Ok();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (!NodeExists(updatedNode.Id))
            {
                return NotFound(new { message = $"Node with ID {updatedNode.Id} not found." });
            }
            else
            {
                Core.Instance.Logger.LogError(ex, "Concurrency error while updating the node.");
                Core.Instance.WriteExceptionToFile(ex);
                return StatusCode(500, "Internal server error. Please check the logs for more details.");
            }
        }
        catch (Exception ex)
        {
            Core.Instance.Logger.LogError(ex, "There was an exception while trying to update the node.");
            Core.Instance.WriteExceptionToFile(ex);
            return StatusCode(500, "Internal server error. Please check the logs for more details.");
        }
    }
    
    [HttpDelete]
    [Route("DeleteNode/{nodeId}")]
    [Authorize]
    public async Task<ActionResult> DeleteNode(int nodeId)
    {
        try
        {
            var nodeToDelete = await context.Nodes.FindAsync(nodeId);
            if (nodeToDelete == null)
            {
                return NotFound(new { message = $"Node with ID {nodeId} not found." });
            }

            context.Nodes.Remove(nodeToDelete);
            await context.SaveChangesAsync();

            string deleteOutgoingCommand = $"sudo iptables -D OUTPUT -d {nodeToDelete.NodeAddress} -p tcp --dport 8000 -j ACCEPT";
            string deleteIncomingCommand = $"sudo iptables -D INPUT -s {nodeToDelete.NodeAddress} -p tcp --sport 8000 -m state --state ESTABLISHED,RELATED -j ACCEPT";

            await ExecuteShellCommandAsync(deleteOutgoingCommand);
            await ExecuteShellCommandAsync(deleteIncomingCommand);

            return Ok();
        }
        catch (Exception ex)
        {
            Core.Instance.Logger.LogError(ex, "There was an exception while trying to delete the node.");
            Core.Instance.WriteExceptionToFile(ex);
            return StatusCode(500, "Internal server error. Please check the logs for more details.");
        }
    }
    
    [HttpGet]
    [Route("CheckNodeState/{nodeId}")]
    [Authorize]
    public async Task<ActionResult<NodeState>> CheckNodeState(int nodeId)
    {
        try
        {
            var node = await context.Nodes.FindAsync(nodeId);
            if (node == null)
            {
                return NotFound(new { message = $"Node with ID {nodeId} not found." });
            }

            return Ok(node.NodeState);
        }
        catch (Exception ex)
        {
            Core.Instance.Logger.LogError(ex, "There was an exception while trying to check the node state.");
            Core.Instance.WriteExceptionToFile(ex);
            return StatusCode(500, "Internal server error. Please check the logs for more details.");
        }
    }
    
    [HttpPut]
    [Route("SetNodeState/{nodeId}")]
    [Authorize]
    public async Task<IActionResult> SetNodeState(int nodeId, [FromBody] NodeState newState)
    {
        try
        {
            var node = await context.Nodes.FindAsync(nodeId);
            if (node == null)
            {
                return NotFound(new { message = $"Node with ID {nodeId} not found." });
            }

            node.NodeState = newState;
            context.Entry(node).State = EntityState.Modified;
            await context.SaveChangesAsync();

            return Ok();
        }
        catch (Exception ex)
        {
            Core.Instance.Logger.LogError(ex, "There was an exception while trying to set the node state.");
            Core.Instance.WriteExceptionToFile(ex);
            return StatusCode(500, "Internal server error. Please check the logs for more details.");
        }
    }
    
    private bool NodeExists(int id)
    {
        return context.Nodes.Any(e => e.Id == id);
    }
    
    private async Task ExecuteShellCommandAsync(string command)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = $"-c \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = processStartInfo };
        process.Start();

        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Command execution failed: {error}");
        }
    }
}