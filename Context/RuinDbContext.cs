using DragonAPI.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace DragonAPI.Context;

public sealed class RuinDbContext : DbContext
{

    private DbSet<ValidTokenUser> ValidTokenUsers { get; init; } = null!;
    public required DbSet<GlobalPlayerAccount> GlobalPlayerAccounts { get; init; } = null!;
    public required DbSet<GlobalGroups> GlobalGroups { get; init; } = null!;
    public required DbSet<Token> Tokens { get; init; } = null!;
    public required DbSet<Account> Accounts { get; init; } = null!;
    public required DbSet<Nodes> Nodes { get; init; } = null!;
    public required DbSet<Servers> Servers { get; init; } = null!;
    public DbSet<EnvironmentVariable> EnvironmentVariables { get; init; }
    public DbSet<SpecialArguments> SpecialArguments { get; init; }
    public required DbSet<GameTypes> GameTypes { get; init; } = null!;
    
    public RuinDbContext(DbContextOptions<RuinDbContext> options) : base(options)
    {
        try
        {
            if (Database.GetService<IDatabaseCreator>() is not RelationalDatabaseCreator database) return;
            if (!database.CanConnect()) database.Create();
            if (!database.HasTables()) database.CreateTables();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
}