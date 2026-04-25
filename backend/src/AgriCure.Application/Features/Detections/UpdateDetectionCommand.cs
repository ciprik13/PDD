using AgriCure.Application.Common.DTOs;
using AgriCure.Application.Common.Exceptions;
using AgriCure.Application.Common.Interfaces;
using AgriCure.Domain.Detections;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Severity = AgriCure.Domain.Detections.Severity;

namespace AgriCure.Application.Features.Detections;

public sealed record UpdateDetectionCommand(
    Guid Id,
    long FrameId,
    DateTimeOffset Timestamp,
    Severity Severity,
    IReadOnlyList<ClassPredictionDto> Predictions,
    BoundingBoxDto BoundingBox,
    int InferenceMs,
    bool ConfidenceGatePassed,
    int Row,
    string PlantId,
    double PositionMeters) : IRequest<Unit>;

internal sealed class UpdateDetectionCommandValidator : AbstractValidator<UpdateDetectionCommand>
{
    public UpdateDetectionCommandValidator()
    {
        RuleFor(c => c.Id).NotEqual(Guid.Empty);
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

internal sealed class UpdateDetectionCommandHandler(
    IApplicationDbContext db,
    TimeProvider time)
    : IRequestHandler<UpdateDetectionCommand, Unit>
{
    public async Task<Unit> Handle(UpdateDetectionCommand request, CancellationToken cancellationToken)
    {
        // Load detection without auto-including predictions — we'll replace them via direct delete + insert.
        var detection = await db.Detections
            .IgnoreAutoIncludes()
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException($"Detection '{request.Id}' was not found.");

        var plantExists = await db.Plants.AnyAsync(p => p.Id == request.PlantId, cancellationToken);
        if (!plantExists)
        {
            db.Plants.Add(new Plant
            {
                Id = request.PlantId,
                CreatedAt = time.GetUtcNow(),
            });
        }

        detection.FrameId = request.FrameId;
        detection.Timestamp = request.Timestamp;
        detection.Severity = request.Severity;
        detection.BoundingBox = new BoundingBox(
            request.BoundingBox.X,
            request.BoundingBox.Y,
            request.BoundingBox.Width,
            request.BoundingBox.Height,
            request.BoundingBox.DepthMeters,
            request.BoundingBox.AffectedAreaPercent);
        detection.InferenceMs = request.InferenceMs;
        detection.ConfidenceGatePassed = request.ConfidenceGatePassed;
        detection.Row = request.Row;
        detection.PlantId = request.PlantId;
        detection.PositionMeters = request.PositionMeters;

        // Replace predictions: hard-delete the old set, insert the new one.
        await db.Predictions
            .Where(p => p.DetectionId == request.Id)
            .ExecuteDeleteAsync(cancellationToken);

        var rank = 0;
        foreach (var p in request.Predictions)
        {
            db.Predictions.Add(new ClassPrediction
            {
                Id = Guid.NewGuid(),
                DetectionId = detection.Id,
                DiseaseClass = p.DiseaseClass,
                Confidence = p.Confidence,
                Label = p.Label,
                Rank = rank++,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
