using System.Text.Json;
using seedapi.Models;

namespace seedapi.Services;

public class SeedFileService
{
    private SeedFile? _seed;
    private string? _loadedFilename;
    private DateTime? _loadedAt;
    private readonly string _dataDir;
    private readonly ILogger<SeedFileService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public SeedFileService(IConfiguration config, ILogger<SeedFileService> logger)
    {
        _dataDir = config["SeedFile:DataDir"] ?? "/data";
        _logger = logger;
    }

    public void Reload()
    {
        var now = DateTime.UtcNow;
        var candidates = new List<(string Path, SeedFile Seed)>();

        foreach (var path in Directory.GetFiles(_dataDir, "*.json"))
        {
            SeedFile? seed;
            try
            {
                seed = JsonSerializer.Deserialize<SeedFile>(File.ReadAllText(path), JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Deleting unreadable seed file {Path}", path);
                File.Delete(path);
                continue;
            }

            if (seed is null)
            {
                _logger.LogWarning("Deleting empty seed file {Path}", path);
                File.Delete(path);
                continue;
            }

            candidates.Add((path, seed));
        }

        // Premiere passe : trouver le meilleur seed actif (ValidFrom le plus recent
        // parmi ceux en cours de validite), independamment de l'ordre d'enumeration.
        string? bestPath = null;
        SeedFile? best = null;
        foreach (var (path, seed) in candidates)
        {
            var isActive = seed.ValidFrom <= now && seed.ExpireAt > now;
            if (isActive && (best is null || seed.ValidFrom > best.ValidFrom))
            {
                best = seed;
                bestPath = path;
            }
        }

        // Seconde passe : supprime tout ce qui est expire ou supplante par le meilleur,
        // mais laisse intacts les fichiers pas encore actifs (ValidFrom > now).
        foreach (var (path, seed) in candidates)
        {
            if (path == bestPath || seed.ValidFrom > now) continue;

            _logger.LogInformation(
                "Deleting stale seed file {Path} (valid_from={ValidFrom}, expire_at={ExpireAt})",
                path, seed.ValidFrom, seed.ExpireAt);
            File.Delete(path);
        }

        if (best is not null)
        {
            _seed = best;
            _loadedFilename = Path.GetFileName(bestPath);
            _loadedAt = DateTime.UtcNow;
        }
    }


    public SeedFile? Seed => _seed;
    public bool IsLoaded => _seed is not null;
    public MetaData Meta => new MetaData(_loadedFilename, _loadedAt, _seed.ValidFrom, _seed.ExpireAt);
}


public record MetaData (
    string filename,
    DateTime? loaded_at,
    DateTime valid_from,
    DateTime expire_at
);