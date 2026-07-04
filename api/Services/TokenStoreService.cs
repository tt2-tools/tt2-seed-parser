using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace seedapi.Services;

public record TokenEntry(
    string Id,
    string Name,
    string TokenHash,
    DateTime CreatedAt
);

public record TokenSummary(
    string Id,
    string Name,
    DateTime CreatedAt
);

file record TokenFile(
    [property: JsonPropertyName("tokens")] List<TokenEntry> Tokens
);

public class TokenStoreService : IDisposable
{
    private readonly string _filePath;
    private readonly ILogger<TokenStoreService> _logger;
    private readonly Lock _lock = new();
    private List<TokenEntry> _tokens = [];
    private FileSystemWatcher? _watcher;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public TokenStoreService(IConfiguration config, ILogger<TokenStoreService> logger)
    {
        _filePath = config["TokenFile:Path"] ?? "/data/tokens.json";
        _logger = logger;
        EnsureFileExists();
        Load();
        WatchFile();
    }

    public bool IsValid(string token)
    {
        var incomingHash = Convert.FromBase64String(Hash(token));
        lock (_lock)
            return _tokens.Any(t =>
                CryptographicOperations.FixedTimeEquals(
                    incomingHash,
                    Convert.FromBase64String(t.TokenHash)));
    }

    public IReadOnlyList<TokenSummary> List()
    {
        lock (_lock) return _tokens.Select(t => new TokenSummary(t.Id, t.Name, t.CreatedAt)).ToList();
    }

    public (TokenEntry entry, string rawToken) Generate(string name)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var entry = new TokenEntry(
            Id: Guid.NewGuid().ToString(),
            Name: name,
            TokenHash: Hash(rawToken),
            CreatedAt: DateTime.UtcNow
        );

        lock (_lock)
        {
            _tokens.Add(entry);
            Persist();
        }

        return (entry, rawToken);
    }

    public bool Revoke(string id)
    {
        lock (_lock)
        {
            var removed = _tokens.RemoveAll(t => t.Id == id) > 0;
            if (removed) Persist();
            return removed;
        }
    }

    private static string Hash(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private void EnsureFileExists()
    {
        if (File.Exists(_filePath)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var empty = JsonSerializer.Serialize(new TokenFile([]), JsonOptions);
        File.WriteAllText(_filePath, empty);
        SetFilePermissions(_filePath);
    }

    private void Load()
    {
        try
        {
            var file = JsonSerializer.Deserialize<TokenFile>(File.ReadAllText(_filePath), JsonOptions);
            lock (_lock) _tokens = file?.Tokens ?? [];
            _logger.LogInformation("Token store loaded: {Count} token(s)", _tokens.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load token store from {Path}", _filePath);
        }
    }

    private void Persist()
    {
        var tmp = _filePath + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(new TokenFile(_tokens), JsonOptions));
        File.Move(tmp, _filePath, overwrite: true);
        SetFilePermissions(_filePath);
    }

    private static void SetFilePermissions(string path)
    {
        try { File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite); }
        catch { /* not supported on Windows */ }
    }

    private void WatchFile()
    {
        var dir = Path.GetDirectoryName(_filePath)!;
        var file = Path.GetFileName(_filePath);
        _watcher = new FileSystemWatcher(dir, file)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
            EnableRaisingEvents = true,
        };
        _watcher.Changed += (_, _) => Load();
        _watcher.Renamed += (_, _) => Load();
    }

    public void Dispose() => _watcher?.Dispose();
}
