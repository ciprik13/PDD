using System.Globalization;
using AgriCure.Application.Common.Auth;
using AgriCure.Application.Common.Exceptions;
using AgriCure.Application.Common.Interfaces;
using AgriCure.Application.Common.Pictures;
using AgriCure.Application.Features.Passports.Common;
using AgriCure.Domain.Detections;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgriCure.Application.Features.Passports;

/// <summary>
/// Full life history for a single plant: identity, current status, a chronological
/// event timeline (scans + applied treatments) and a daily severity sparkline.
/// Tenant-scoped: agriculture users may only read passports for plants they own.
/// Throws <see cref="NotFoundException"/> when the plant doesn't exist or isn't visible.
/// </summary>
public sealed record GetPlantPassportQuery(string PlantId) : IRequest<PlantPassportDto>;

internal sealed class GetPlantPassportQueryHandler(
    IApplicationDbContext db,
    ICurrentUserAccessor currentUser,
    IPictureService pictures)
    : IRequestHandler<GetPlantPassportQuery, PlantPassportDto>
{
    public async Task<PlantPassportDto> Handle(
        GetPlantPassportQuery request, CancellationToken cancellationToken)
    {
        var plant = await db.Plants
            .FirstOrDefaultAsync(p => p.Id == request.PlantId, cancellationToken);

        if (plant is null ||
            (!currentUser.IsAdmin && plant.OwnerUserId != currentUser.RequireUserId()))
        {
            throw new NotFoundException($"Plant '{request.PlantId}' was not found.");
        }

        var detections = await db.Detections
            .Where(d => d.PlantId == plant.Id)
            .OrderBy(d => d.Timestamp)
            .ToListAsync(cancellationToken);

        // Each detection may have one linked frame. Join the link table to the pictures in a
        // single query so the timeline can show the captured photo per detection event.
        var detectionIds = detections.Select(d => d.Id).ToArray();
        var linkedPictures = await db.DetectionPictures
            .Where(dp => detectionIds.Contains(dp.DetectionId))
            .Join(
                db.Pictures,
                dp => dp.PictureId,
                pic => pic.Id,
                (dp, pic) => new { dp.DetectionId, Picture = pic })
            .ToListAsync(cancellationToken);
        var imageUrlByDetection = linkedPictures
            .ToDictionary(x => x.DetectionId, x => pictures.GetUrl(x.Picture));

        var applied = await db.AppliedTreatments
            .Where(a => a.PlantId == plant.Id)
            .OrderBy(a => a.AppliedAt)
            .ToListAsync(cancellationToken);

        var treatmentIds = applied.Select(a => a.TreatmentId).Distinct().ToArray();
        var treatments = await db.Treatments
            .Where(t => treatmentIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, cancellationToken);

        var events = new List<PassportEventDto>
        {
            new(
                Id: $"created-{plant.Id}",
                Type: PassportEventType.Created,
                Timestamp: plant.CreatedAt,
                Title: "Passport created",
                Description: "Plant registered in the system."),
        };

        foreach (var d in detections)
        {
            var top = d.Predictions.OrderBy(p => p.Rank).FirstOrDefault();
            var confPct = top is null ? 0 : Math.Round(top.Confidence * 100, 1);
            var area = Math.Round(d.BoundingBox?.AffectedAreaPercent ?? 0, 1);
            var label = top?.Label ?? "Scan";

            var (type, title, description) = d.Severity switch
            {
                Severity.Critical => (
                    PassportEventType.Disease,
                    $"{label} detected — high severity",
                    $"{confPct}% confidence · {area}% leaf area"),
                Severity.Warning => (
                    PassportEventType.Symptom,
                    $"{label} — early symptom",
                    $"{confPct}% confidence · flagged for watch"),
                _ => (
                    PassportEventType.Healthy,
                    "Healthy scan — no disease",
                    $"{confPct}% confidence"),
            };

            events.Add(new PassportEventDto(
                Id: $"det-{d.Id}",
                Type: type,
                Timestamp: d.Timestamp,
                Title: title,
                Description: description,
                DetectionId: d.Id,
                Confidence: top?.Confidence,
                Severity: d.Severity,
                ImageUrl: imageUrlByDetection.GetValueOrDefault(d.Id)));
        }

        foreach (var a in applied)
        {
            var name = treatments.TryGetValue(a.TreatmentId, out var t) ? t.Name : "Treatment";
            var dosage = t?.Dosage is { Length: > 0 } dose ? $" · {dose}" : string.Empty;
            var note = string.IsNullOrWhiteSpace(a.Notes) ? string.Empty : $" · {a.Notes}";

            events.Add(new PassportEventDto(
                Id: $"trt-{a.Id}",
                Type: PassportEventType.Treatment,
                Timestamp: a.AppliedAt,
                Title: $"{name} applied",
                Description: $"Treatment applied{dosage}{note}"));
        }

        var severityHistory = detections
            .GroupBy(d => d.Timestamp.UtcDateTime.Date)
            .Select(g => new SeverityPointDto(
                g.Key.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Math.Round(g.Max(DiseaseIntensity), 1)))
            .OrderBy(p => p.Date)
            .ToArray();

        var latest = detections.LastOrDefault();

        return new PlantPassportDto(
            Id: $"passport-{plant.Id}",
            PlantIndex: ParsePlantIndex(plant.Id),
            Row: latest?.Row ?? 0,
            CreatedAt: plant.CreatedAt,
            CurrentStatus: latest?.Severity ?? Severity.Healthy,
            Events: events.OrderByDescending(e => e.Timestamp).ToArray(),
            SeverityHistory: severityHistory);
    }

    // 0–100 "how diseased" score for the sparkline: healthy scans contribute 0,
    // diseased scans contribute their affected-area percentage (a real measurement),
    // falling back to top-prediction confidence when no area was recorded.
    private static double DiseaseIntensity(Detection d)
    {
        if (d.Severity == Severity.Healthy)
        {
            return 0;
        }

        var area = d.BoundingBox?.AffectedAreaPercent ?? 0;
        if (area > 0)
        {
            return area;
        }

        var top = d.Predictions.OrderBy(p => p.Rank).FirstOrDefault();
        return (top?.Confidence ?? 0) * 100;
    }

    private static int ParsePlantIndex(string plantId)
    {
        var digits = new string(plantId.Where(char.IsDigit).ToArray());
        return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx)
            ? idx
            : 0;
    }
}
