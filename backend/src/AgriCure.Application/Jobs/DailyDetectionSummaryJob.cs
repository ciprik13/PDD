using Microsoft.Extensions.Logging;

namespace AgriCure.Application.Jobs;

/// <summary>
/// Sample recurring job — logs only. Real implementation deferred.
/// Proves Hangfire wiring without depending on real detection data.
/// </summary>
public sealed class DailyDetectionSummaryJob(ILogger<DailyDetectionSummaryJob> logger)
{
    public Task ExecuteAsync()
    {
        logger.LogInformation(
            "DailyDetectionSummaryJob ran at {Timestamp:O} — placeholder, real impl pending.",
            DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }
}
