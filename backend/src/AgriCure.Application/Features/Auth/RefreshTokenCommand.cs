using AgriCure.Application.Common.Auth;
using AgriCure.Application.Common.DTOs;
using AgriCure.Application.Common.Interfaces;
using AgriCure.Domain.Identity;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AgriCure.Application.Features.Auth;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResultDto>;

internal sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(c => c.RefreshToken).NotEmpty();
    }
}

internal sealed class RefreshTokenCommandHandler(
    IIdentityService identity,
    IJwtTokenService tokens,
    IApplicationDbContext db,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider time)
    : IRequestHandler<RefreshTokenCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(
        RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var existing = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken, cancellationToken)
            ?? throw new AuthenticationFailedException("Invalid refresh token.");

        if (!existing.IsActive)
        {
            throw new AuthenticationFailedException("Refresh token is no longer active.");
        }

        var user = await identity.GetUserContextAsync(existing.UserId, cancellationToken)
            ?? throw new AuthenticationFailedException("User no longer exists.");

        var now = time.GetUtcNow();
        var accessToken = tokens.IssueAccessToken(user.UserId, user.Email, user.Roles);
        var newRefreshValue = tokens.GenerateRefreshTokenValue();
        var newRefreshExpiresAt = now.AddDays(jwtOptions.Value.RefreshTokenDays);

        existing.RevokedAt = now;
        existing.ReplacedByToken = newRefreshValue;

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.UserId,
            Token = newRefreshValue,
            CreatedAt = now,
            ExpiresAt = newRefreshExpiresAt,
        });

        await db.SaveChangesAsync(cancellationToken);

        return new AuthResultDto(
            accessToken.Token,
            accessToken.ExpiresAt,
            newRefreshValue,
            newRefreshExpiresAt);
    }
}
