using AgriCure.Application.Common.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgriCure.Application.Features.Auth;

public sealed record LogoutCommand(string RefreshToken) : IRequest<Unit>;

internal sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(c => c.RefreshToken).NotEmpty();
    }
}

internal sealed class LogoutCommandHandler(
    IApplicationDbContext db,
    TimeProvider time)
    : IRequestHandler<LogoutCommand, Unit>
{
    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var existing = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, cancellationToken);

        if (existing is { IsActive: true })
        {
            existing.RevokedAt = time.GetUtcNow();
            await db.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
