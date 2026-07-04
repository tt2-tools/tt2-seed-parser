using System.Security.Cryptography;
using System.Text;
using seedapi.Services;

namespace seedapi.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var adminKey = Environment.GetEnvironmentVariable("ADMIN_KEY");

        app.MapGet("/admin/tokens", (HttpContext ctx, TokenStoreService svc) =>
        {
            if (!IsAuthorized(ctx, adminKey)) return Results.Unauthorized();
            return Results.Ok(svc.List());
        });

        app.MapPost("/admin/tokens", async (HttpContext ctx, TokenStoreService svc) =>
        {
            if (!IsAuthorized(ctx, adminKey)) return Results.Unauthorized();

            CreateTokenRequest? body;
            try { body = await ctx.Request.ReadFromJsonAsync<CreateTokenRequest>(); }
            catch { return Results.BadRequest("invalid JSON"); }

            if (string.IsNullOrWhiteSpace(body?.Name))
                return Results.BadRequest("name is required");

            var (entry, rawToken) = svc.Generate(body.Name);
            return Results.Ok(new { entry.Id, entry.Name, Token = rawToken, entry.CreatedAt });
        });

        app.MapDelete("/admin/tokens/{id}", (HttpContext ctx, TokenStoreService svc, string id) =>
        {
            if (!IsAuthorized(ctx, adminKey)) return Results.Unauthorized();
            return svc.Revoke(id) ? Results.NoContent() : Results.NotFound();
        });
    }

    private static bool IsAuthorized(HttpContext ctx, string? adminKey)
    {
        if (string.IsNullOrEmpty(adminKey)) return false;
        var a = Encoding.UTF8.GetBytes(ctx.Request.Headers.Authorization.ToString());
        var b = Encoding.UTF8.GetBytes($"Bearer {adminKey}");
        if (a.Length != b.Length) return false;
        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}

file record CreateTokenRequest(string? Name);
