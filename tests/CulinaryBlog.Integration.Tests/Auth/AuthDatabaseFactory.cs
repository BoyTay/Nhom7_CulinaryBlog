using System.Collections.Concurrent;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Serilog.Core;
using Serilog.Events;

namespace CulinaryBlog.Integration.Tests.Auth;

public sealed class AuthDatabaseFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private AuthTestDatabase? _database;
    private readonly CapturingSink _sink = new();
    public string AdminEmail { get; } = $"seed-{Guid.NewGuid():N}@example.test";
    public string AdminPassword { get; } = $"Aa1!{Guid.NewGuid():N}";
    public IReadOnlyList<LogEvent> Logs => _sink.Entries.ToArray();

    public async Task InitializeAsync() => _database = await AuthTestDatabase.CreateAsync();
    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Admin:Email"] = AdminEmail,
            ["Admin:Password"] = AdminPassword,
            ["Seq:ServerUrl"] = "",
            ["Hangfire:Enabled"] = "false",
        }));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(_database!.ConnectionString));
            services.AddTransient<IStartupFilter, TestClientAddressStartupFilter>();
            services.AddSingleton<ILogEventSink>(_sink);
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (_database is not null) await _database.DisposeAsync();
    }

    private sealed class CapturingSink : ILogEventSink
    {
        public ConcurrentQueue<LogEvent> Entries { get; } = new();
        public void Emit(LogEvent logEvent) => Entries.Enqueue(logEvent);
    }
}
