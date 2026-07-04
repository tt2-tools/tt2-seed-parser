using seedapi.Services;

namespace seedapi.Endpoints;

public static class SeedEndpoints
{
    public static void MapSeedEndpoints(this WebApplication app)
    {

        app.MapGet("/seed/meta", (SeedFileService svc) =>
            svc.IsLoaded ? Results.Ok(svc.Meta) : Results.NotFound("No valid seed file found"));

        app.MapGet("/seed/raids", (SeedFileService svc) =>
        {
            if (!svc.IsLoaded) return Results.NotFound("No valid seed file found");
            var raids = svc.Seed!.Raids.AsEnumerable();
            return Results.Ok(raids.ToList());
        });

        app.MapGet("/seed/raids/{tier}/{level}", (SeedFileService svc, int tier, int level) =>
        {
            if (!svc.IsLoaded) return Results.NotFound("No valid seed file found");
            var raid = svc.Seed!.Raids.FirstOrDefault(r => r.Tier == tier && r.Level == level);
            return raid is not null
                ? Results.Ok(raid)
                : Results.NotFound($"No raid found for tier {tier}, level {level}");
        });
    }
}
