using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskApi.Data;
using TaskApi.Logic.Tasks;
using Testcontainers.PostgreSql;

namespace TaskApi.Tests;

public sealed class TaskApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:17").Build();

    private WebApplicationFactory<Program>? _factory;

    public HttpClient Client { get; private set; } = null!;
    public RecordingPublisher Publisher { get; } = new();

    public IServiceProvider Services => _factory!.Services;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");

                builder.UseSetting(
                    "ConnectionStrings:TaskDb",
                    _postgres.GetConnectionString());

                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<ITaskEventPublisher>();
                    services.AddSingleton<ITaskEventPublisher>(Publisher);
                });
            });

        await using var scope = Services.CreateAsyncScope();

        var db = scope.ServiceProvider
            .GetRequiredService<TaskDbContext>();

        await db.Database.MigrateAsync();

        Client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();

        try
        {
            if (_factory is not null)
                await _factory.DisposeAsync();
        }
        finally
        {
            await _postgres.DisposeAsync();
        }
    }
}
