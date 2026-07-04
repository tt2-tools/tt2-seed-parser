namespace seedapi.Services;

public class SeedReloadHostedService(SeedFileService svc, ILogger<SeedReloadHostedService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        svc.Reload();
        logger.LogInformation("Initial seed loaded: {IsLoaded}", svc.IsLoaded);

        while (!ct.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextMidnight = now.Date.AddDays(1);
            await Task.Delay(nextMidnight - now, ct);

            svc.Reload();
            logger.LogInformation("Nightly seed reload: {IsLoaded}", svc.IsLoaded);
        }
    }
}
