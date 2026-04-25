using AgriCure.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace AgriCure.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<RefreshToken> RefreshTokens { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
