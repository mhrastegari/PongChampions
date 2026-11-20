using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PongChampions.Api.Common.Dtos.Room;
using PongChampions.Api.Services;
using PongChampions.Data;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Testcontainers.PostgreSql;

namespace PongChampions.Tests;

[CollectionDefinition("backend")]
public sealed class BackendCollection : ICollectionFixture<BackendFixture>
{
}
public sealed class BackendFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:17").Build();
    public WebApplicationFactory<Program> Factory { get; private set; } = null!;
    public string ConnectionString => postgres.GetConnectionString();
    public static JsonSerializerOptions Json { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public async Task InitializeAsync()
    {
        await postgres.StartAsync();

        await using var context = NewContext();

        await context.Database.MigrateAsync();
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["Jwt:Key"] = "test-only-signing-key-over-thirty-two-bytes-long",
                ["Jwt:Issuer"] = "PongChampions.Tests",
                ["Jwt:Audience"] = "PongChampions.Tests",
                ["Https:Redirect"] = "false",
                ["RateLimits:AuthPerMinute"] = "1000",
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Microsoft"] = "Warning"
            }));
        });

        using var client = Factory.CreateClient();

        (await client.GetAsync("/health/ready")).EnsureSuccessStatusCode();
    }

    public AppDbContext NewContext(params IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .AddInterceptors(interceptors)
            .Options;

        return new AppDbContext(options);
    }

    public HttpClient Client(string? token = null)
    {
        var client = Factory.CreateClient();

        if (token is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    public async Task<string> RegisterAsync()
    {
        using var client = Client();
        var username = "p" + Guid.NewGuid().ToString("N")[..20];

        var credentials = new
        {
            username,
            password = "TestPassword_123",
            displayName = "Test player"
        };

        (await client.PostAsJsonAsync("/api/auth/register", credentials)).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/auth/login", credentials);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }

    public async Task<RoomDto> CreateRoomAsync(string token)
    {
        using var client = Client(token);
        var response = await client.PostAsync("/api/rooms", null);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<RoomDto>(Json))!;
    }

    public async Task<JoinRoomResponseDto> JoinAsync(string code, string? token = null)
    {
        using var client = Client(token);
        var response = await client.PostAsync($"/api/rooms/{code}/join", null);

        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<JoinRoomResponseDto>(Json))!;
    }

    public HubConnection Hub(string? token = null)
    {
        return new HubConnectionBuilder().WithUrl("http://localhost/hubs/rooms", options =>
        {
            options.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
            options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
            if (token is not null)
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
        }).Build();
    }

    public async Task DisposeAsync()
    {
        if (Factory is not null)
            await Factory.DisposeAsync();

        await postgres.DisposeAsync();
    }
}
