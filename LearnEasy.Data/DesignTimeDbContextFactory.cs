using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LearnEasy.Data;

/// <summary>
/// Lets <c>dotnet ef migrations add ...</c> build the context without spinning
/// up the whole app. The connection string here is only used at design time;
/// runtime uses the Aspire-injected one. Override with the env var
/// <c>LEARNEASY_DESIGN_CONNECTION</c> if your local Postgres differs.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LearnEasyDbContext>
{
    public LearnEasyDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("LEARNEASY_DESIGN_CONNECTION")
                   ?? "Host=localhost;Port=5432;Database=learneasy;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<LearnEasyDbContext>()
            .UseNpgsql(conn)
            .Options;

        return new LearnEasyDbContext(options);
    }
}
