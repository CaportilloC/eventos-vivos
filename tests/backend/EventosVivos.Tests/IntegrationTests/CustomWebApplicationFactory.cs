using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using EventosVivos.Infrastructure.Data;

namespace EventosVivos.Tests.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory for integration testing against a real SQL Server Docker instance.
///
/// Usage:
/// 1. Start Docker SQL Server: `docker compose -f docker/docker-compose.yml up -d`
/// 2. Run tests: `dotnet test`
///
/// The factory applies EF migrations (including seed data) on first use,
/// then recreates the database between test classes via IAsyncLifetime lifecycle.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>
    /// Flags whether the database has been initialized for this factory instance.
    /// </summary>
    private bool _dbInitialized;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }

    /// <summary>
    /// Runs AFTER the host is created but BEFORE the first test.
    /// Uses a separate scope to apply migrations (drop + recreate for clean state).
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_dbInitialized) return;

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EventosVivosDbContext>();

        // Drop and recreate for a clean state per test class run
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();

        _dbInitialized = true;
    }

    /// <summary>
    /// Cleanup after all tests complete.
    /// Does NOT stop Docker; Docker is managed separately by the user.
    /// </summary>
    public new Task DisposeAsync() => Task.CompletedTask;
}
