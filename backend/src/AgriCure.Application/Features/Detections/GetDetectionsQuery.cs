using AgriCure.Application.Common.Auth;
using AgriCure.Application.Common.DTOs;
using AgriCure.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgriCure.Application.Features.Detections;

public sealed record GetDetectionsQuery(int Limit = 20) : IRequest<IReadOnlyList<DetectionDto>>;

internal sealed class GetDetectionsQueryValidator : AbstractValidator<GetDetectionsQuery>
{
    public GetDetectionsQueryValidator()
    {
        RuleFor(q => q.Limit).GreaterThan(0);
    }
}

internal sealed class GetDetectionsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserAccessor currentUser)
    : IRequestHandler<GetDetectionsQuery, IReadOnlyList<DetectionDto>>
{
    public const int MaxLimit = 200;

    public async Task<IReadOnlyList<DetectionDto>> Handle(
        GetDetectionsQuery request, CancellationToken cancellationToken)
    {
        var clamped = Math.Min(request.Limit, MaxLimit);

        var query = db.Detections.AsQueryable();

        if (!currentUser.IsAdmin)
        {
            var userId = currentUser.RequireUserId();
            query = query.Where(d => db.Plants
                .Any(p => p.Id == d.PlantId && p.OwnerUserId == userId));
        }

        var detections = await query
            .OrderByDescending(d => d.Timestamp)
            .Take(clamped)
            .ToListAsync(cancellationToken);

        return detections.Select(d => d.ToDto()).ToArray();
    }
}
