namespace AgriCure.Application.Features.Dashboard.Common;

/// <summary>Headline numbers for the dashboard, aggregated from real detection data.</summary>
public sealed record DashboardStatsDto(
    int DetectionsToday,
    int DetectionsDelta,
    double AvgConfidence,
    int RowsScanned,
    int TotalRows,
    int PlantsTracked);
