using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PongChampions.Api.Common;
using PongChampions.Api.Hubs;
using PongChampions.Api.Services;
using PongChampions.Data;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddJsonConsole();

var jwtSection = builder.Configuration.GetSection("Jwt");

builder.Services.AddOptions<JwtSettings>()
    .BindConfiguration("Jwt")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.Key) &&
            Encoding.UTF8.GetByteCount(options.Key) >= 32 &&
            !string.IsNullOrWhiteSpace(options.Issuer) &&
            !string.IsNullOrWhiteSpace(options.Audience),
        "Configure Jwt:Key (at least 32 bytes), Jwt:Issuer and Jwt:Audience using environment variables or user secrets.")
    .ValidateOnStart();

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddOptions<GameOptions>()
    .BindConfiguration("Game")
    .Validate(
        options => options.TickMilliseconds is >= 10 and <= 1000 &&
            options.ReconnectGraceSeconds >= 5 &&
            options.LobbyLifetimeMinutes >= 1,
        "Game timing options are outside the supported range.")
    .ValidateOnStart();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection.");

    options.UseNpgsql(connectionString);
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"] ?? "")),
            ClockSkew = TimeSpan.FromSeconds(15),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Path.StartsWithSegments("/hubs/rooms") &&
                    context.Request.Query["access_token"].FirstOrDefault() is { Length: > 0 } token)
                    context.Token = token;

                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                if (!Guid.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out _))
                    context.Fail("Invalid subject.");

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RegisteredPlayer", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context => context.User.FindFirstValue("identity_kind") != "guest");
    });
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

builder.Services.AddOpenApi();

builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 4096;
    options.AddFilter<InputRateFilter>();
});

builder.Services.AddSingleton<RoomCoordinator>();
builder.Services.AddSingleton<RoomEvents>();
builder.Services.AddSingleton<GuestTokenService>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IMatchService, MatchService>();
builder.Services.AddScoped<RoomMaintenance>();

builder.Services.AddSingleton<IGameSessionService, GameSessionService>();

builder.Services.AddHostedService<GameBackgroundService>();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("postgres", tags: ["ready"]);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        if (origins.Length > 0)
        {
            policy.WithOrigins(origins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
        context => RateLimitPartition.GetTokenBucketLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new()
            {
                TokenLimit = 200,
                TokensPerPeriod = 100,
                ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                AutoReplenishment = true
            }));

    options.AddPolicy("auth",
        context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new()
            {
                PermitLimit = builder.Configuration.GetValue("RateLimits:AuthPerMinute", 10),
                Window = TimeSpan.FromMinutes(1),
                AutoReplenishment = true
            }));

    options.OnRejected = async (context, token) =>
    {
        await Results.Problem(statusCode: 429, title: "Too many requests.")
            .ExecuteAsync(context.HttpContext);
    };
});

var app = builder.Build();

if (args.Contains("--migrate-only"))
{
    using var scope = app.Services.CreateScope();

    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    await context.Database.MigrateAsync();

    return;
}

app.UseExceptionHandler();

app.UseStatusCodePages(async context =>
{
    await Results.Problem(statusCode: context.HttpContext.Response.StatusCode)
        .ExecuteAsync(context.HttpContext);
});

if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
    app.UseHsts();

if (builder.Configuration.GetValue("Https:Redirect", true))
    app.UseHttpsRedirection();

app.UseCors();

app.UseRateLimiter();

app.UseAuthentication();

// Anonymous spectators/joins are allowed, but a supplied invalid credential is rejected.
app.Use(async (context, next) =>
{
    if ((context.Request.Headers.ContainsKey("Authorization") ||
         (context.Request.Path.StartsWithSegments("/hubs/rooms") &&
          context.Request.Query.ContainsKey("access_token"))) &&
        context.User.Identity?.IsAuthenticated != true)
    {
        await Results.Problem(statusCode: 401, title: "Invalid or expired bearer token.")
            .ExecuteAsync(context);

        return;
    }

    await next(context);
});

app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseStaticFiles();
}

app.MapControllers();

app.MapHub<RoomHub>("/hubs/rooms", options => options.CloseOnAuthenticationExpiration = true);

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

await app.RunAsync();

public partial class Program
{
}
