using AgriCure.Application.Common.DTOs;
using AgriCure.Application.Common.Interfaces;
using AgriCure.Domain.Detections;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Severity = AgriCure.Domain.Detections.Severity;

namespace AgriCure.Application.Features.Detections;

public sealed record CreateDetectionCommand(
    long FrameId,
    DateTimeOffset Timestamp,
    Severity Severity,
    IReadOnlyList<ClassPredictionDto> Predictions,
    BoundingBoxDto BoundingBox,
    int InferenceMs,
    bool ConfidenceGatePassed,
    int Row,
    string PlantId,
    double PositionMeters) : IRequest<Guid>;

internal sealed class CreateDetectionCommandValidator : AbstractValidator<CreateDetectionCommand>
{
    public CreateDetectionCommandValidator()
    {
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

internal sealed class CreateDetectionCommandHandler(
    IApplicationDbContext db,
    TimeProvider time)
    : IRequestHandler<CreateDetectionCommand, Guid>
{
    public async Task<Guid> Handle(CreateDetectionCommand request, CancellationToken cancellationToken)
    {
        var plantExists = await db.Plants.AnyAsync(p => p.Id == request.PlantId, cancellationToken);
        if (!plantExists)
        {
            db.Plants.Add(new Plant
            {
                Id = request.PlantId,
                CreatedAt = time.GetUtcNow(),
            });
        }

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
            CreatedAt = time.GetUtcNow(),
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

        db.Detections.Add(detection);
        await db.SaveChangesAsync(cancellationToken);
        return detection.Id;
    }
}
