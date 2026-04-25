namespace AgriCure.Api.IntegrationTests.Detections;

internal static class DetectionTestData
{
    public static object BuildCreateBody(string? plantId = null) => new
    {
        frameId = Random.Shared.NextInt64(1, 1_000_000),
        timestamp = DateTimeOffset.UtcNow,
        severity = "warning",
        predictions = new[]
        {
            new { diseaseClass = "early_blight", confidence = 0.85, label = "Early Blight" },
            new { diseaseClass = "healthy", confidence = 0.10, label = "Healthy" },
        },
        boundingBox = new
        {
            x = 0.3,
            y = 0.4,
            width = 0.2,
            height = 0.25,
            depthMeters = 0.5,
            affectedAreaPercent = 12.5,
        },
        inferenceMs = 32,
        confidenceGatePassed = true,
        row = 7,
        plantId = plantId ?? $"P-{Guid.NewGuid():N}".Substring(0, 16),
        positionMeters = 12.4,
    };

    public static object BuildUpdateBody(Guid id, string? plantId = null) => new
    {
        id,
        frameId = 9999L,
        timestamp = DateTimeOffset.UtcNow,
        severity = "critical",
        predictions = new[]
        {
            new { diseaseClass = "late_blight", confidence = 0.95, label = "Late Blight" },
        },
        boundingBox = new
        {
            x = 0.5,
            y = 0.5,
            width = 0.3,
            height = 0.3,
            depthMeters = 0.4,
            affectedAreaPercent = 18.0,
        },
        inferenceMs = 28,
        confidenceGatePassed = true,
        row = 7,
        plantId = plantId ?? $"P-{Guid.NewGuid():N}".Substring(0, 16),
        positionMeters = 12.4,
    };
}
