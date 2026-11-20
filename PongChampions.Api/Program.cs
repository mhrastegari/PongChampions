using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PongChampions.Api.Common;
using PongChampions.Api.Hubs;
using PongChampions.Api.Services;
using PongChampions.Data;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var jwt = builder.Configuration.GetSection("Jwt");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"] ?? "")),
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

builder.Services.AddAuthorization(o => o.AddPolicy(
    "RegisteredPlayer",
    p => p.RequireAuthenticatedUser()
        .RequireAssertion(c => c.User.FindFirstValue("identity_kind") != "guest")));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler<ApiExceptionHandler>();

builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddSignalR();

builder.Services.AddSingleton<RoomCoordinator>();

builder.Services.AddSingleton<RoomEvents>();

builder.Services.AddSingleton<GuestTokenService>();

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IRoomService, RoomService>();

builder.Services.AddScoped<IMatchService, MatchService>();

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddOptions<GameOptions>()
    .BindConfiguration("Game")
    .Validate(
        o => o.TickMilliseconds is >= 10 and <= 1000 && o.ReconnectGraceSeconds >= 5 &&
            o.LobbyLifetimeMinutes >= 1,
        "Game timing options are outside the supported range.")
    .ValidateOnStart();

builder.Services.AddSingleton<IGameSessionService, GameSessionService>();

builder.Services.AddHostedService<GameBackgroundService>();

var app = builder.Build();

app.UseExceptionHandler();

app.UseStatusCodePages(async context => await Results.Problem(statusCode: context.HttpContext.Response.StatusCode).ExecuteAsync(context.HttpContext));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();

// Anonymous spectators/joins are allowed, but a supplied invalid credential is rejected.
app.Use(async (context, next) =>
{
    if ((context.Request.Headers.ContainsKey("Authorization") ||
        (context.Request.Path.StartsWithSegments("/hubs/rooms") &&
        context.Request.Query.ContainsKey("access_token"))) &&
        context.User.Identity?.IsAuthenticated != true)
    {
        await Results.Problem(statusCode: 401, title: "Invalid or expired bearer token.").ExecuteAsync(context);

        return;
    }

    await next(context);
});

app.UseAuthorization();

app.MapControllers();

app.MapHub<RoomHub>("/hubs/rooms", o => o.CloseOnAuthenticationExpiration = true);

app.Run();
