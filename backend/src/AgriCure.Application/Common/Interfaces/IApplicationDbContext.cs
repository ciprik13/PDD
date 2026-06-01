using AgriCure.Domain.Detections;
using AgriCure.Domain.Identity;
using AgriCure.Domain.Pictures;
using Microsoft.EntityFrameworkCore;

namespace AgriCure.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<Detection> Detections { get; }

    DbSet<ClassPrediction> Predictions { get; }

    DbSet<Plant> Plants { get; }

    DbSet<Picture> Pictures { get; }

    DbSet<ApiKey> ApiKeys { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
