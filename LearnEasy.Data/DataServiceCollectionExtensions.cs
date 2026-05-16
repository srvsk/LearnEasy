using LearnEasy.Core.Abstractions;
using LearnEasy.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LearnEasy.Data;

public static class DataServiceCollectionExtensions
{
    /// <summary>
    /// Registers the DbContext (PostgreSQL) and repositories. Pass the
    /// connection string Aspire injects as <c>ConnectionStrings:learneasydb</c>.
    /// </summary>
    public static IServiceCollection AddLearnEasyData(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<LearnEasyDbContext>(opt =>
            opt.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));

        services.AddScoped<IWordRepository, WordRepository>();
        services.AddScoped<IProgressRepository, ProgressRepository>();

        return services;
    }

    /// <summary>
    /// Brings the database up to date and applies seed content. Call once on
    /// app startup; idempotent and safe to call repeatedly.
    ///
    /// Uses EF migrations when any exist (the production path — run
    /// <c>dotnet ef migrations add InitialCreate</c> once). If none have been
    /// generated yet it falls back to <c>EnsureCreated</c> so a fresh clone
    /// still runs end-to-end without the EF tooling installed.
    /// </summary>
    public static async Task EnsureDatabaseAsync(this IServiceProvider provider, CancellationToken ct = default)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LearnEasyDbContext>();

        var hasMigrations = db.Database.GetMigrations().Any();
        if (hasMigrations)
            await db.Database.MigrateAsync(ct);
        else
            await db.Database.EnsureCreatedAsync(ct);
    }
}
