using AgriCure.Domain.Detections;
using AgriCure.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace AgriCure.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<Detection> Detections { get; }

    DbSet<ClassPrediction> Predictions { get; }

    DbSet<Plant> Plants { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
