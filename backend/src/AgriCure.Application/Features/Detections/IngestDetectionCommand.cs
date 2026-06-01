using AgriCure.Application.Common.Auth;
using AgriCure.Application.Common.DTOs;
using AgriCure.Application.Common.Exceptions;
using AgriCure.Application.Common.Interfaces;
using AgriCure.Application.Common.Pictures;
using AgriCure.Domain.Detections;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Severity = AgriCure.Domain.Detections.Severity;

namespace AgriCure.Application.Features.Detections;

public sealed record IngestDetectionCommand(
    byte[] ImageBytes,
    string MimeType,
    long FrameId,
    DateTimeOffset Timestamp,
    Severity Severity,
    IReadOnlyList<ClassPredictionDto> Predictions,
    BoundingBoxDto BoundingBox,
    int InferenceMs,
    bool ConfidenceGatePassed,
    int Row,
    string PlantId,
    double PositionMeters) : IRequest<IngestDetectionResult>;

public sealed record IngestDetectionResult(
    Guid DetectionId,
    Guid? PictureId,
    string PlantId,
    bool IsNewPlant,
    bool IsDuplicate);

internal sealed class IngestDetectionCommandValidator : AbstractValidator<IngestDetectionCommand>
{
    private static readonly string[] AllowedMimeTypes = ["image/png", "image/jpeg", "image/webp"];

    public IngestDetectionCommandValidator()
    {
        RuleFor(c => c.ImageBytes)
            .NotNull()
            .Must(b => b.Length > 0)
                .WithMessage("Image must be non-empty.");

        RuleFor(c => c.MimeType)
            .NotEmpty()
            .Must(m => AllowedMimeTypes.Contains(m, StringComparer.OrdinalIgnoreCase))
                .WithMessage(c => $"MimeType '{c.MimeType}' is not allowed.");

        RuleFor(c => c.FrameId).GreaterThan(0);
        RuleFor(c => c.Timestamp).NotEqual(default(DateTimeOffset));
        RuleFor(c => c.Severity).IsInEnum();

        RuleFor(c => c.Predictions).NotEmpty();
        RuleForEach(c => c.Predictions).ChildRules(p =>
        {
            p.RuleFor(x => x.DiseaseClass).IsInEnum();
            p.RuleFor(x => x.Confidence).InclusiveBetween(0, 1);
            p.RuleFor(x => x.Label).NotEmpty().MaximumLength(128);
        });

        RuleFor(c => c.BoundingBox).NotNull();
        RuleFor(c => c.BoundingBox.X).InclusiveBetween(0, 1).When(c => c.BoundingBox is not null);
        RuleFor(c => c.BoundingBox.Y).InclusiveBetween(0, 1).When(c => c.BoundingBox is not null);
        RuleFor(c => c.BoundingBox.Width).InclusiveBetween(0, 1).When(c => c.BoundingBox is not null);
        RuleFor(c => c.BoundingBox.Height).InclusiveBetween(0, 1).When(c => c.BoundingBox is not null);

        RuleFor(c => c.InferenceMs).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Row).GreaterThan(0);
        RuleFor(c => c.PlantId).NotEmpty().MaximumLength(64);
        RuleFor(c => c.PositionMeters).GreaterThanOrEqualTo(0);
    }
}

internal sealed class IngestDetectionCommandHandler(
    IApplicationDbContext db,
    IPictureService pictures,
    ICurrentUserAccessor currentUser,
    TimeProvider time,
    ILogger<IngestDetectionCommandHandler> logger)
    : IRequestHandler<IngestDetectionCommand, IngestDetectionResult>
{
    private readonly IApplicationDbContext _db = db;
    private readonly IPictureService _pictures = pictures;
    private readonly ICurrentUserAccessor _currentUser = currentUser;
    private readonly TimeProvider _time = time;
    private readonly ILogger<IngestDetectionCommandHandler> _logger = logger;

    public async Task<IngestDetectionResult> Handle(
        IngestDetectionCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.RequireUserId();

        // 1. Dedup check (before image upload)
        var existing = await _db.Detections
            .FirstOrDefaultAsync(
                d => d.PlantId == request.PlantId && d.FrameId == request.FrameId,
                cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            var existingPictureId = await _db.DetectionPictures
                .Where(dp => dp.DetectionId == existing.Id)
                .Select(dp => (Guid?)dp.PictureId)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return new IngestDetectionResult(
                existing.Id,
                existingPictureId,
                existing.PlantId,
                IsNewPlant: false,
                IsDuplicate: true);
        }

        // 2. Plant resolution
        var plant = await _db.Plants
            .FirstOrDefaultAsync(p => p.Id == request.PlantId, cancellationToken)
            .ConfigureAwait(false);

        var isNewPlant = false;
        if (plant is null)
        {
            plant = new Plant
            {
                Id = request.PlantId,
                OwnerUserId = userId,
                CreatedAt = _time.GetUtcNow(),
            };
            _db.Plants.Add(plant);
            isNewPlant = true;
        }
        else if (plant.OwnerUserId != userId)
        {
            throw new ConflictException(
                $"Plant '{request.PlantId}' is owned by another agriculture user.");
        }

        // 3. Image upload
        var picture = await _pictures.InsertAsync(
            request.ImageBytes,
            request.MimeType,
            seoFilename: $"frame-{request.FrameId}",
            altAttribute: null,
            titleAttribute: null,
            cancellationToken).ConfigureAwait(false);

        // 4. DB writes
        var detection = new Detection
        {
            Id = Guid.NewGuid(),
            FrameId = request.FrameId,
            Timestamp = request.Timestamp,
            Severity = request.Severity,
            BoundingBox = new BoundingBox(
                request.BoundingBox.X,
                request.BoundingBox.Y,
                request.BoundingBox.Width,
                request.BoundingBox.Height,
                request.BoundingBox.DepthMeters,
                request.BoundingBox.AffectedAreaPercent),
            InferenceMs = request.InferenceMs,
            ConfidenceGatePassed = request.ConfidenceGatePassed,
            Row = request.Row,
            PlantId = request.PlantId,
            PositionMeters = request.PositionMeters,
            CreatedAt = _time.GetUtcNow(),
            Predictions = request.Predictions
                .Select((p, idx) => new ClassPrediction
                {
                    Id = Guid.NewGuid(),
                    DiseaseClass = p.DiseaseClass,
                    Confidence = p.Confidence,
                    Label = p.Label,
                    Rank = idx,
                })
                .ToList(),
        };

        _db.Detections.Add(detection);
        _db.DetectionPictures.Add(new DetectionPicture
        {
            DetectionId = detection.Id,
            PictureId = picture.Id,
            CreatedAt = _time.GetUtcNow(),
        });

        try
        {
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (IsUniquePlantFrameViolation(ex))
        {
            _logger.LogInformation(
                "Concurrent ingest race for ({PlantId}, {FrameId}); returning existing detection.",
                request.PlantId, request.FrameId);

            var raced = await _db.Detections
                .FirstOrDefaultAsync(
                    d => d.PlantId == request.PlantId && d.FrameId == request.FrameId,
                    cancellationToken)
                .ConfigureAwait(false);

            var racedPictureId = raced is null
                ? null
                : await _db.DetectionPictures
                    .Where(dp => dp.DetectionId == raced.Id)
                    .Select(dp => (Guid?)dp.PictureId)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);

            return new IngestDetectionResult(
                raced!.Id,
                racedPictureId,
                raced.PlantId,
                IsNewPlant: false,
                IsDuplicate: true);
        }

        return new IngestDetectionResult(
            detection.Id,
            picture.Id,
            plant.Id,
            IsNewPlant: isNewPlant,
            IsDuplicate: false);
    }

    private static bool IsUniquePlantFrameViolation(DbUpdateException ex)
    {
        if (ex.InnerException is null)
        {
            return false;
        }
        if (ex.InnerException.Data["ConstraintName"] is string name &&
            string.Equals(name, "IX_Detections_PlantId_FrameId", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return ex.InnerException.Message.Contains("IX_Detections_PlantId_FrameId", StringComparison.OrdinalIgnoreCase);
    }
}
