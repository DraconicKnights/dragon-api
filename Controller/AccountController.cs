using System.Security.Cryptography;
using System.Text;
using DragonAPI.Context;
using DragonAPI.CreationModel;
using DragonAPI.Model;
using DragonAPI.Server;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DragonAPI.Controller;

/// <summary>
/// Account controller, deals with the account creation and handling 
/// </summary>
/// <param name="context"></param>
[ApiController]
[Route("api/[controller]")]
public class AccountController(RuinDbContext context) : ControllerBase
{
    [HttpPost("register")]
    [Authorize]
    public async Task<IActionResult> Register([FromBody]CreateAccountModel model)
    {
        if (context.Accounts.Any(e => e.AccountName == model.Username))
            return BadRequest("User name is already taken");

        if (context.Accounts.Any(e => e.AccountName == model.Email))
            return BadRequest("Email is already registered with us");
            
        var account = new Account() 
        { 
            AccountName = model.Username, 
            AccountPassword = ConvertToHashedPassword(model.Password),
            AccountEmail = model.Email,
            ServiceTerms = model.Terms,
            IsStaff = false,
            AdminOverride = false
        };

        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, account);
    }

    [HttpPost("login")]
    [Authorize]
    public async Task<ActionResult<Account>> Login([FromBody]LoginModel model)
    {
        var user = await context.Accounts.Where(u => u.AccountEmail == model.Email).FirstOrDefaultAsync();
        if (user == null)
        {
            Core.Instance.Logger.Log(LogLevel.Information, "Response: Account Login Failed Reason: Account doesn't exist within our Database");
            return BadRequest("Account is not Registered with us");
        }

        var hashedPasswordModel = ConvertToHashedPassword(model.Password);
        if (user.AccountPassword != hashedPasswordModel)
        {
            Core.Instance.Logger.Log(LogLevel.Information, "Response: Account Login Failed Reason: Account information doesn't match");
            return BadRequest("Invalid Login Credentials");
        }

        return user;
    }

    [HttpPost("update")]
    [Authorize]
    public async Task<IActionResult> Update([FromBody]CreateAccountModel model)
    {
        var user = await context.Accounts.Where(u => u.AccountName == model.Username).FirstOrDefaultAsync();

        if (user == null) return NotFound();
        
        user.AccountPassword = ConvertToHashedPassword(model.Password);
        user.AccountEmail = model.Email;

        context.Entry(user).State = EntityState.Modified;
        await context.SaveChangesAsync();

        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteAccount(long id)
    {
        var account = await context.Accounts.FindAsync(id);

        if (account == null) return NotFound();

        context.Accounts.Remove(account);
        await context.SaveChangesAsync();

        return Ok();
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<Account>> GetAccount(long id)
    {
        var account = await context.Accounts.Include(ac => ac.Servers)
            .SingleOrDefaultAsync(account => account.Id == id);

        if (account == null)
        {
            return NotFound();
        }

        return account;
    }

    private string ConvertToHashedPassword(string password)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}