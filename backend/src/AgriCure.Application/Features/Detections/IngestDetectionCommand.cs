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

    public Task<IngestDetectionResult> Handle(IngestDetectionCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException("Implemented in Task 4.");
    }
}
