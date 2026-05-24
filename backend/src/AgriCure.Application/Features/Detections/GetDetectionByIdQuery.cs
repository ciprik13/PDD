using AgriCure.Application.Common.Auth;
using AgriCure.Application.Common.DTOs;
using AgriCure.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgriCure.Application.Features.Detections;

public sealed record GetDetectionByIdQuery(Guid Id) : IRequest<DetectionDto?>;

internal sealed class GetDetectionByIdQueryValidator : AbstractValidator<GetDetectionByIdQuery>
{
    public GetDetectionByIdQueryValidator()
    {
        RuleFor(q => q.Id).NotEqual(Guid.Empty);
    }
}

internal sealed class GetDetectionByIdQueryHandler(
    IApplicationDbContext db,
    ICurrentUserAccessor currentUser)
    : IRequestHandler<GetDetectionByIdQuery, DetectionDto?>
{
    public async Task<DetectionDto?> Handle(
        GetDetectionByIdQuery request, CancellationToken cancellationToken)
    {
        var query = db.Detections.AsQueryable();

        if (!currentUser.IsAdmin)
        {
            var userId = currentUser.RequireUserId();
            query = query.Where(d => db.Plants
                .Any(p => p.Id == d.PlantId && p.OwnerUserId == userId));
        }

        var detection = await query
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        return detection?.ToDto();
    }
}
