using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using seedapi.Endpoints;
using seedapi.Middleware;
using seedapi.Services;

var builder = WebApplication.CreateBuilder(args);

var adminKey = Environment.GetEnvironmentVariable("ADMIN_KEY");
if (string.IsNullOrEmpty(adminKey) || adminKey.Length < 16)
    throw new InvalidOperationException("ADMIN_KEY env var must be set and at least 16 characters.");

builder.Services.AddSingleton<SeedFileService>();
builder.Services.AddSingleton<TokenStoreService>();
builder.Services.AddHostedService<SeedReloadHostedService>();

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

var allowedOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS")
    ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? [];

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()));

builder.Services.AddRateLimiter(o =>
{
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 60,
            }));
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

app.UseCors();
app.UseRateLimiter();
app.UseMiddleware<ApiKeyMiddleware>();

app.MapSeedEndpoints();
app.MapAdminEndpoints();

app.Run();
